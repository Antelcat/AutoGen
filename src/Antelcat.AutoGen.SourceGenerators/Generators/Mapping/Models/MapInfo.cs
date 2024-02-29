using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Mail;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapInfo
{
    public MapInfo(IMethodSymbol Method, ITypeSymbol From, ITypeSymbol To)
    {
        this.Method      = Method;
        this.From        = From;
        this.To          = To;
        IsSelf           = SymbolEqualityComparer.Default.Equals(From, Method.ContainingType);
        FromAccess       = GetAccess(Method, From);
        ToAccess         = GetAccess(Method, To);
        MethodAttributes = Method.GetAttributes();
        Methods          = Method.ContainingType.GetMembers().OfType<IMethodSymbol>().ToList();
        var autoMap = MethodAttributes
            .First(static x => x.AttributeClass!.HasFullyQualifiedMetadataName(typeof(AutoMapAttribute).FullName))
            .ToAttribute<AutoMapAttribute>();
        if (autoMap.Extra != null)
        {
            Extra = Methods.Where(x => autoMap.Extra.Contains(x.Name)    &&
                                       !(Method.IsStatic && !x.IsStatic) &&
                                       x.Parameters.Length switch
                                       {
                                           0 => true,
                                           1 => x.Parameters[0].Type.Is(From) || x.Parameters[0].Type.Is(To),
                                           2 => x.Parameters[0].Type.Is(From) && x.Parameters[1].Type.Is(To) ||
                                                x.Parameters[0].Type.Is(To)   && x.Parameters[1].Type.Is(From),
                                           _ => false
                                       });
        }

        Provider = new MapProvider(Method, From)
        {
            RequiredAccess = autoMap.FromAccess,
            Attributes = IsSelf
                ? MethodAttributes
                : MethodAttributes.Concat(Method.Parameters[0].GetAttributes()).ToImmutableArray()
        };
        Receiver = new MapReceiver(Method, To)
        {
            RequiredAccess = autoMap.ToAccess,
            Attributes     = Method.GetReturnTypeAttributes()
        };
    }

    public bool IsSelf { get; }

    public Accessibility                 FromAccess       { get; }
    public Accessibility                 ToAccess         { get; }
    public ImmutableArray<AttributeData> MethodAttributes { get; }
    public IEnumerable<IMethodSymbol>    Extra            { get; } = [];
    private List<IMethodSymbol> Methods { get; }

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
                        args = [symbol.Parameters[0].Type.Is(From) ? Provider.ArgName : Receiver.ArgName];
                        break;
                    case 2:
                        args =
                        [
                            symbol.Parameters[0].Type.Is(From) ? Provider.ArgName : Receiver.ArgName,
                            symbol.Parameters[1].Type.Is(To) ? Receiver.ArgName : Provider.ArgName
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

    public IMethodSymbol Method { get; init; }
    public ITypeSymbol   From   { get; init; }
    public ITypeSymbol   To     { get; init; }


    public BlockSyntax Map()
    {
        var between = MethodAttributes.Select(static x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(typeof(MapBetweenAttribute).FullName)
                    ? x.ToAttribute<MapBetweenAttribute>()
                    : null!)
            .Where(static x => x != null);

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
                provide?.MetadataName ?? x.FromProperty,
                receive?.MetadataName ?? x.ToProperty,
                method);
        }).Concat(receives.Select(x =>
            {
                var provide = provides.FirstOrDefault(p =>
                    Compatible(p.MetadataName, x.MetadataName));
                return provide != null
                    ? new MapPair(provide.MetadataName, x.MetadataName, null)
                    : null;
            }).Where(x => x != null)
        ).Select(p => p!.Call(Provider.ArgName)).ToList();


        var statements = new List<StatementSyntax>
        {
            ParseStatement(
                $$"""
                  var {{Receiver.ArgName}} = {{GetCtor()}}
                  {
                  {{string.Join("\n", pairs)}}
                  };
                  """)
        };

        statements.AddRange(CallExtra.Select(x => ParseStatement(x)));

        return Block(statements.Append(ParseStatement($"return {Receiver.ArgName};")));
    }

    private static bool Compatible(string one, string another) =>
        string.Equals(
            one.Replace("_", ""),
            another.Replace("_", ""),
            StringComparison.OrdinalIgnoreCase);

    private string GetCtor()
    {
        var className = Receiver.Type.GetFullyQualifiedName();

        var mapCtor = MethodAttributes.FirstOrDefault(static x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(typeof(MapConstructorAttribute).FullName))?
            .ToAttribute<MapConstructorAttribute>();

        if (mapCtor != null) return New(mapCtor.PropertyNames.Select((x, i) =>
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

        if (ctors.Any(x => x.Parameters.Length == 0)) return New();
        foreach (var ctor in ctors.OrderBy(x => x.Parameters.Length))
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

        string New(IEnumerable<string>? args = null) => $"new {className}({string.Join(", ", args ?? [])})";
    }
}