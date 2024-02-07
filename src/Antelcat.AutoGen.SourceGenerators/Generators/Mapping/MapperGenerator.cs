using static Microsoft.CodeAnalysis.Diagnostic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping;

[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
    private static readonly string AutoMap        = typeof(AutoMapAttribute).FullName!;
    private static readonly string MapBetween     = typeof(MapBetweenAttribute).FullName!;
    private static readonly string MapExclude     = typeof(MapExcludeAttribute).FullName!;
    private static readonly string MapInclude     = typeof(MapIncludeAttribute).FullName!;
    private static readonly string MapIgnore      = typeof(MapIgnoreAttribute).FullName!;
    private static readonly string MapConstructor = typeof(MapConstructorAttribute).FullName!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoMap,
            static (ctx, t) => ctx is MethodDeclarationSyntax,
            static (ctx, _) => ctx);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            static (ctx, tuple) =>
            {
                foreach (var group in tuple.Right
                             .GroupBy(static x =>
                                 (x.TargetSymbol as IMethodSymbol)!.ContainingType, SymbolEqualityComparer.Default))
                {
                    var @class  = (group.Key as INamedTypeSymbol)!;
                    var partial = @class.PartialClass();
                    foreach (var syntax in group)
                    {
                        var method   = (syntax.TargetSymbol as IMethodSymbol)!;
                        var location = syntax.TargetNode.GetLocation();
                        if (@class.TypeKind == TypeKind.Interface)
                        {
                            ctx.ReportDiagnostic(
                                Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                    location,
                                    nameof(AutoMapAttribute),
                                    "not be declared in an interface"
                                ));
                            continue;
                        }

                        ITypeSymbol fromType;
                        ITypeSymbol toType;
                        var         mode = FilterMethodAndReport(method, ctx, location);
                        switch (mode)
                        {
                            case MethodMode.ArgumentError:
                                ctx.ReportDiagnostic(
                                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                        location,
                                        nameof(AutoMapAttribute),
                                        "zero or one parameter"
                                    ));
                                continue;
                            case MethodMode.MapSelf:
                                if (@class.IsStatic)
                                {
                                    ctx.ReportDiagnostic(
                                        Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                            location,
                                            nameof(AutoMapAttribute),
                                            "not in a static class"
                                        ));
                                    continue;
                                }

                                fromType = @class;
                                toType   = method.ReturnType;
                                break;
                            case MethodMode.MapBetween:
                                fromType = method.Parameters[0].Type;
                                toType   = method.ReturnType;
                                break;
                            case MethodMode.Invalid:
                            default: continue;
                        }

                        var methodSyntax = (syntax.TargetNode as MethodDeclarationSyntax)!;
                        partial = partial.AddMembers(
                            methodSyntax.FullQualifiedPartialMethod(method)
                                .AddGenerateAttribute(typeof(MapperGenerator))
                                .WithBody(GenerateMethod(method, fromType, toType, mode is MethodMode.MapSelf)));
                    }

                    ctx.AddSource($"{nameof(AutoMap)}__.{@class.GetFullyQualifiedName()
                        .Replace("global::", string.Empty)}.g.cs",
                        CompilationUnit()
                            .AddPartialClass(@class, x => partial)
                            .NormalizeWhitespace()
                            .GetText(Encoding.UTF8));
                }
            });
    }

    private static MethodMode FilterMethodAndReport(IMethodSymbol method,
        SourceProductionContext context,
        Location location)
    {
        if (!method.IsPartialDefinition)
        {
            context.ReportDiagnostic(
                Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "should be partial definition"
                ));
        }
        
        if (method.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "not return void"
                ));
            return MethodMode.Invalid;
        }

        switch (method.ReturnType.TypeKind)
        {
            case TypeKind.Interface or TypeKind.Dynamic:
                context.ReportDiagnostic(
                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                        location,
                        nameof(AutoMapAttribute),
                        "return actual type"
                    ));
                return MethodMode.Invalid;
            case TypeKind.Class when method.IsAbstract:
                context.ReportDiagnostic(
                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                        location,
                        nameof(AutoMapAttribute),
                        "return none abstract type"
                    ));
                return MethodMode.Invalid;
        }
     
        return method.Parameters.Length switch
        {
            0 => MethodMode.MapSelf,
            1 => MethodMode.MapBetween,
            _ => MethodMode.ArgumentError
        };
    }

    private enum MethodMode
    {
        Invalid,
        MapSelf,
        MapBetween,
        ArgumentError
    }

    private static BlockSyntax GenerateMethod(
        IMethodSymbol method,
        ITypeSymbol from,
        ITypeSymbol to,
        bool isSelf)
    {
        var fromName = isSelf ? "this" : method.Parameters[0].Name;
        var toName   = isSelf ? "ret" : method.Parameters[0].Name == "ret" ? "retVal" : "ret";

        var fromAccess = GetAccess(method, from);
        var toAccess   = GetAccess(method, to);

        var attrs = method.GetAttributes();

        var configs = GetIncludesAndExcludes(method, attrs, isSelf);

        var extra = method.ContainingType.GetMembers().OfType<IMethodSymbol>().ToList();

        var mapConfig = attrs
            .First(x => x.AttributeClass!.HasFullyQualifiedMetadataName(AutoMap))
            .ToAttribute<AutoMapAttribute>();

        var fromProps = GetProperties(from)
            .Where(x =>
                !x.IsStatic                                                &&
                !x.IsWriteOnly                                             &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.FromAccess) &&
                QualifiedProperty(x, fromAccess, configs.fromExcludes, configs.fromIncludes))
            .ToList();

        var toProps = GetProperties(to)
            .Where(x =>
                !x.IsStatic                                              &&
                !x.IsReadOnly                                            &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.ToAccess) &&
                QualifiedProperty(x, toAccess, configs.toExcludes, configs.toIncludes))
            .ToList();

        var pairs = attrs.Select(static x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(MapBetween)
                    ? x.ToAttribute<MapBetweenAttribute>()
                    : null!)
            .Where(static x => x != null);

        var matches = pairs.Select(x =>
            {
                /*var fromIndex = fromProps.FindIndex(p => p.Name == x.FromProperty);
                if (fromIndex < 0) return (from: null!, to: null, by: null!);*/
                var by      = extra.FirstOrDefault(m => m.Name == x.By);
                var toIndex = toProps.FindIndex(p => p.Name    == x.ToProperty);
                if (toIndex >= 0) toProps.RemoveAt(toIndex);
                /*fromProps.RemoveAt(fromIndex);*/
                return (from: x.FromProperty, to: (string?)x.ToProperty, by);
            })
            .Concat(fromProps.Select(x =>
            {
                var fromProp = x.Name;
                var match = toProps
                    .Select(static (p, i) => (p, i))
                    .FirstOrDefault(y => Compatible(fromProp, y.p.Name));
                if (match.p != null) toProps.RemoveAt(match.i);
                return (from: fromProp, to: match.p?.Name, by: (IMethodSymbol?)null!);
            }))
            .Where(x => x.to != null)
            .ToArray();


        var mapCtor = attrs.FirstOrDefault(static x => x.AttributeClass!.HasFullyQualifiedMetadataName(MapConstructor));

        var ctor = mapCtor == null
            ? GenerateCtor(to, fromName, fromProps)
            : GenerateSpecifiedCtor(to, fromName, mapCtor.ToAttribute<MapConstructorAttribute>().PropertyNames);


        var statements = new List<StatementSyntax>
        {
            ParseStatement(
                $$"""
                  var {{toName}} = {{ctor}}
                  {
                  {{string.Join("\n", matches.Select(x => MapOne(x.from, x.to!, x.by)))}}
                  };
                  """)
        };

        if (mapConfig.Extra is { Length: > 0 })
        {
            statements.AddRange(method.ContainingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => mapConfig.Extra.Contains(x.Name) && (isSelf
                    ? x.Parameters.Length == 1 && x.Parameters[0].Type.Is(to)
                    : x.Parameters.Length == 2      &&
                      x.Parameters[0].Type.Is(from) && x.Parameters[1].Type.Is(to) ||
                      x.Parameters[1].Type.Is(from) && x.Parameters[0].Type.Is(to)))
                .Select(symbol => ParseStatement(isSelf
                    ? $"this.{symbol.Name}({toName});"
                    : $"{symbol.ContainingType.GetFullyQualifiedName()}.{symbol.Name}({
                        (symbol.Parameters[0].Type.Is(from) ? $"{fromName}, {toName}" : $"{toName}, {fromName}")
                    });")));
        }

        return Block(statements.Append(ParseStatement($"return {toName};")));

        string MapOne(string fromProperty, string toProperty, IMethodSymbol? by)
        {
            var arg = $"{fromName}.{fromProperty}";
            return $"{toProperty} = " + (by == null ? arg : Call(by, arg)) + ',';
        }

        static string Call(IMethodSymbol function, string argument) =>
            $"{(!function.IsStatic ? "this" : function.ContainingType.GetFullyQualifiedName())}.{function.Name}({argument})";
    }

    private static string GenerateSpecifiedCtor(
        ITypeSymbol type,
        string fromName,
        IEnumerable<string> specifiedProps) =>
        $"new {type.GetFullyQualifiedName()}({string.Join(", ", specifiedProps.Select(x => $"{fromName}.{x}"))})";

    private static string GenerateCtor(
        ITypeSymbol type,
        string fromName,
        IList<IPropertySymbol> fromProps)
    {
        if (type is not INamedTypeSymbol namedTypeSymbol ||
            namedTypeSymbol.Constructors.Any(x => x.Parameters.Length == 0))
            return $"new {type.GetFullyQualifiedName()}()";
        foreach (var ctor in namedTypeSymbol.Constructors.OrderBy(x => x.Parameters.Length))
        {
            var parameters = ctor.Parameters;
            var matches    = new List<string>();
            foreach (var prop in parameters
                         .Select(parameter => fromProps
                             .FirstOrDefault(x => Compatible(parameter.Name, x.Name))))
            {
                if (prop == null)
                    goto notfound;
                matches.Add($"{fromName}.{prop.Name}");
            }

            return $"new {type.GetFullyQualifiedName()}({string.Join(", ", matches)})";
            notfound: ;
        }

        return $"new {type.GetFullyQualifiedName()}()";
    }

    private static (
        HashSet<string> fromExcludes,
        HashSet<string> toExcludes,
        HashSet<string> fromIncludes,
        HashSet<string> toIncludes
        )
        GetIncludesAndExcludes(IMethodSymbol method,
            ImmutableArray<AttributeData> attributes,
            bool isSelf)
    {
        var fromExcludes = new HashSet<string>();
        var toExcludes   = new HashSet<string>();
        var fromIncludes = new HashSet<string>();
        var toIncludes   = new HashSet<string>();
        var fromAttrs    = isSelf ? attributes : method.Parameters[0].GetAttributes();

        Map(fromAttrs, fromIncludes, fromExcludes);
        Map(method.GetReturnTypeAttributes(), toIncludes, toExcludes);

        return (fromExcludes, toExcludes, fromIncludes, toIncludes);

        static void Map(ImmutableArray<AttributeData> attributes, ISet<string> includes, ISet<string> excludes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass!.ToDisplayString() == MapExclude)
                {
                    var attr = attribute.ToAttribute<MapExcludeAttribute>();
                    var name = attr.Property;
                    excludes.Add(name);
                }

                if (attribute.AttributeClass!.ToDisplayString() == MapInclude)
                {
                    var attr = attribute.ToAttribute<MapIncludeAttribute>();
                    var name = attr.Property;
                    includes.Add(name);
                }
            }
        }
    }

    private static bool QualifiedProperty(IPropertySymbol property,
        Accessibility accessibility,
        ICollection<string> excludes,
        ICollection<string> includes)
    {
        if (excludes.Contains(property.Name)) return false;
        if (includes.Contains(property.Name)) return true;
        return property.DeclaredAccessibility.IsIncludedIn(accessibility)
               && !property.GetAttributes()
                   .Any(a => a.AttributeClass!.HasFullyQualifiedMetadataName(MapIgnore));
    }

    

    private static bool Compatible(string one, string another)
    {
        return string.Equals(
            one.Replace("_", ""),
            another.Replace("_", ""),
            StringComparison.OrdinalIgnoreCase);
    }
    
    
    private static IEnumerable<IPropertySymbol> GetProperties(ITypeSymbol type)
    {
        var props = type.GetMembers().OfType<IPropertySymbol>();
        if (type.BaseType == null) return props;
        return props.Concat(GetProperties(type.BaseType))
            .Distinct(PropNameComparator.Comparator);
    }

    private class PropNameComparator : IEqualityComparer<IPropertySymbol>
    {
        public bool Equals(IPropertySymbol x, IPropertySymbol y) => x.MetadataName == y.MetadataName;

#pragma warning disable RS1024
        public int GetHashCode(IPropertySymbol obj) => obj.GetHashCode();
#pragma warning restore RS1024

        public static PropNameComparator Comparator { get; } = new();
    }
}