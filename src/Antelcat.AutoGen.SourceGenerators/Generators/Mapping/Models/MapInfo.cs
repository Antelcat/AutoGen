using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapInfo
{
    public MapInfo(IMethodSymbol method, ITypeSymbol from, ITypeSymbol to)
    {
        IsSelf           = SymbolEqualityComparer.Default.Equals(from, method.ContainingType);
        MethodAttributes = method.GetAttributes();
        Methods          = method.ContainingType.GetMembers().OfType<IMethodSymbol>().ToList();
        var autoMap = MethodAttributes.GetAttributes<AutoMapAttribute>().First();
        if (autoMap.Extra != null)
        {
            Extra = Methods.Where(x => autoMap.Extra.Contains(x.Name)    &&
                                       !(method.IsStatic && !x.IsStatic) &&
                                       x.Parameters.Length switch
                                       {
                                           0 => true,
                                           1 => x.Parameters[0].Type.Is(from) || x.Parameters[0].Type.Is(to),
                                           2 => x.Parameters[0].Type.Is(from) && x.Parameters[1].Type.Is(to) ||
                                                x.Parameters[0].Type.Is(to)   && x.Parameters[1].Type.Is(from),
                                           _ => false
                                       });
        }

        Provider = new MapProvider(method, from)
        {
            RequiredAccess = autoMap.FromAccess,
            Attributes = IsSelf
                ? MethodAttributes
                : MethodAttributes.Concat(method.Parameters[0].GetAttributes()).ToImmutableArray()
        };
        Receiver = new MapReceiver(method, to)
        {
            RequiredAccess = autoMap.ToAccess,
            Attributes     = method.GetReturnTypeAttributes()
        };
    }

    public bool IsSelf { get; }

    public  ImmutableArray<AttributeData> MethodAttributes { get; }
    public  IEnumerable<IMethodSymbol>    Extra            { get; } = [];
    private List<IMethodSymbol>           Methods          { get; }

    public IEnumerable<string> CallExtra
    {
        get
        {
            foreach (var symbol in Extra)
            {
                string[] args;
                switch (symbol.Parameters.Length)
                {
                    case 0:
                        args = [];
                        break;
                    case 1:
                        args = [symbol.Parameters[0].Type.Is(Provider.Type) ? Provider.ArgName : Receiver.ArgName];
                        break;
                    case 2:
                        args =
                        [
                            symbol.Parameters[0].Type.Is(Provider.Type) ? Provider.ArgName : Receiver.ArgName,
                            symbol.Parameters[1].Type.Is(Receiver.Type) ? Receiver.ArgName : Provider.ArgName
                        ];
                        break;
                    default:
                        continue;
                }

                yield return symbol.Call(args) + ';';
            }
        }
    }

    public MapSide Provider { get; }

    public MapSide Receiver { get; }

    public BlockSyntax Map()
    {
        var between = MethodAttributes.GetAttributes<MapBetweenAttribute>();
        var provides = Provider.RequiredProperties.ToList();
        var receives = Receiver.RequiredProperties.ToList();
        var pairs = between.Select(x =>
            {
                var method = x.By == null
                    ? null
                    : Methods.FirstOrDefault(m => m.Name == x.By);
                var receive = receives.FirstOrDefault(p => p.MetadataName == x.ToProperty);
                var provide = provides.FirstOrDefault(p => p.MetadataName == x.FromProperty);
                if (receive != null) receives.Remove(receive);

                return new MapPair(
                    receive?.MetadataName ?? x.ToProperty,
                    provide?.MetadataName ?? x.FromProperty,
                    method);
            }).Concat(receives
                .Select(x =>
                {
                    var provide = provides.FirstOrDefault(p => Compatible(p.MetadataName, x.MetadataName));
                    return new MapPair(x.MetadataName, provide?.MetadataName);
                }))
            .Select(p => p!.Call(Provider.ArgName))
            .ToList();


        var statements = new List<StatementSyntax>
        {
            ParseStatement(
                $$"""
                  var {{Receiver.ArgName}} = {{Ctor()}}
                  {
                  {{string.Join("\n", pairs)}}
                  };
                  """)
        };

        statements.AddRange(CallExtra.Select(static x => ParseStatement(x)));

        return Block(statements.Append(ParseStatement($"return {Receiver.ArgName};")));
    }

    private static bool Compatible(string one, string another) =>
        string.Equals(
            one.Replace("_", ""),
            another.Replace("_", ""),
            StringComparison.OrdinalIgnoreCase);

    private string Ctor()
    {
        var mapCtor = MethodAttributes.FirstOrDefault(static x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(typeof(MapConstructorAttribute).FullName))?
            .ToAttribute<MapConstructorAttribute>();

        if (mapCtor != null)
            return New(mapCtor.PropertyNames.Select((x, i) =>
            {
                var ret = $"{Provider.ArgName}.{x}";
                if (mapCtor.Bys == null || mapCtor.Bys.Length <= i) return ret;
                var method = Methods.FirstOrDefault(m => m.Name == mapCtor.Bys[i]);
                return method switch
                {
                    { Parameters.Length: 0 } _ => method.Call(),
                    { Parameters.Length: 1 } _ => method.Call(ret),
                    _                          => ret
                };
            }));

        var ctors = Receiver.Type
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static x => x.MethodKind == MethodKind.Constructor)
            .ToList();

        if (ctors.Any(static x => x.Parameters.Length == 0)) return New();
        foreach (var ctor in ctors.OrderBy(static x => x.Parameters.Length))
        {
            var parameters = ctor.Parameters;
            var matches    = new List<string>();
            foreach (var prop in parameters
                         .Select(parameter => Provider.AvailableProperties
                             .FirstOrDefault(x => Compatible(parameter.Name, x.Name))))
            {
                if (prop == null) goto notfound;
                matches.Add($"{Provider.ArgName}.{prop.Name}");
            }

            return New(matches);
            notfound: ;
        }

        return New();

        string New(IEnumerable<string>? args = null) =>
            $"new {Receiver.Type.GetFullyQualifiedName()}({string.Join(", ", args ?? [])})";
    }
}