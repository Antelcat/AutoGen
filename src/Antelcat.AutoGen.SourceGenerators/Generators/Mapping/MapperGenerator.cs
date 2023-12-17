using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;
using Type = Feast.CodeAnalysis.CompileTime.Type;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping;

[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
    private static readonly string AutoMap    = typeof(AutoMapAttribute).FullName!;
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
            static (ctx, t) => ctx);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            static (ctx, tuple) =>
            {
                foreach (var group in tuple.Right
                             .GroupBy(static x =>
                                 (x.TargetSymbol as IMethodSymbol)!.ContainingType, SymbolEqualityComparer.Default))
                {
                    var @class = (group.Key as INamedTypeSymbol)!;

                    var partial = ClassDeclaration(@class.Name)
                        .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PartialKeyword)));

                    foreach (var syntax in group)
                    {
                        var method   = (syntax.TargetSymbol as IMethodSymbol)!;
                        var location = syntax.TargetNode.GetLocation();
                        if (@class.TypeKind == TypeKind.Interface)
                        {
                            ctx.ReportDiagnostic(
                                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
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
                                    Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                        location,
                                        nameof(AutoMapAttribute),
                                        "zero or one parameter"
                                    ));
                                continue;
                            case MethodMode.MapSelf:
                                if (@class.IsStatic)
                                {
                                    ctx.ReportDiagnostic(
                                        Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
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

                        partial = partial.AddMembers(
                            (syntax.TargetNode as MethodDeclarationSyntax)!
                            .WithAttributeLists([])
                            .AddGenerateAttribute(typeof(MapperGenerator))
                            .WithSemicolonToken(default)
                            .WithBody(GenerateMethod(method, fromType, toType, mode is MethodMode.MapSelf)));
                    }

                    var unit = CompilationUnit()
                        .AddMembers(
                            NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
                                .WithLeadingTrivia(Header)
                                .AddMembers(partial));
                    ctx.AddSource($"{@class.Name}.g.cs", unit.NormalizeWhitespace().GetText(Encoding.UTF8));
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
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "should be partial definition"
                ));
        }

        if (method.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "not return void"
                ));
            return MethodMode.Invalid;
        }

        if (method.ReturnType.TypeKind == TypeKind.Interface)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "return actual type"
                ));
            return MethodMode.Invalid;
        }
        
        if (method.ReturnType is { TypeKind: TypeKind.Class, IsAbstract: true })
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
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

        var configs = GetExcludes(attrs, from, to);

        var mapConfig = attrs
            .First(x => x.AttributeClass!.HasFullyQualifiedMetadataName(AutoMap))
            .ToAttribute<AutoMapAttribute>();
        
        var fromProps = from.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                !x.IsWriteOnly                                             &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.ExportFrom) &&
                QualifiedProperty(x, fromAccess, configs.fromExcludes, configs.fromIncludes))
            .ToList();

        var toProps = to.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                !x.IsReadOnly                                            &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.ExportTo) &&
                QualifiedProperty(x, toAccess, configs.toExcludes, configs.toIncludes))
            .ToList();

        var pairs = attrs.Select(static x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(MapBetween)
                    ? x.ToAttribute<MapBetweenAttribute>()
                    : null)
            .Where(static x => x != null)
            .ToList();

        var matches = fromProps.Select(x =>
            {
                var fromProp = x.Name;
                var config   = pairs.FirstOrDefault(a => a!.FromProperty == fromProp || a.ToProperty == fromProp);
                if (config != null)
                {
                    var another = config.FromProperty == fromProp ? config.ToProperty : config.FromProperty;
                    if (toProps.Any(p => p.Name == another))
                    {
                        return (from: fromProp, to: another);
                    }
                }

                var match = toProps.FirstOrDefault(y => Compatible(fromProp, y.Name));
                return (from: fromProp, to: match?.Name);
            })
            .Where(x => x.to != null)
            .ToArray();

        var mapCtor = attrs.FirstOrDefault(static x => x.AttributeClass!.HasFullyQualifiedMetadataName(MapConstructor));
        if (mapCtor != null)
        {
            try
            {

                var c = typeof(MapConstructorAttribute)
                    .GetConstructors()
                    .First();
                var param = c
                    .GetParameters();
                var args = mapCtor.ConstructorArguments
                    .Select((x, i) => x.GetArgumentValue(param[i].ParameterType))
                    .ToArray();
                var ins = Activator.CreateInstance(typeof(MapConstructorAttribute), args);
                var publicProps = typeof(MapConstructorAttribute)
                    .GetProperties(global::System.Reflection.BindingFlags.Public |
                                   global::System.Reflection.BindingFlags.Instance)
                    .Where(static x => x.CanWrite)
                    .ToDictionary(static x => x.Name,static x => x);
                foreach (var argument in mapCtor.NamedArguments)
                {
                    if (!publicProps.TryGetValue(argument.Key, out var prop)) continue;
                    prop.SetValue(ins, argument.Value.GetArgumentValue(prop.PropertyType));
                }

                var attr = mapCtor.ToAttribute<MapConstructorAttribute>();
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case ArgumentException:
                    case MissingMethodException:
                        break;
                    default:
                        break;
                }

            }
        }
        var ctor = mapCtor == null
            ? GenerateCtor(to, fromName, fromProps)
            : GenerateSpecifiedCtor(to, fromName, mapCtor.ToAttribute<MapConstructorAttribute>().PropertyNames);
        
        var statements = new List<StatementSyntax>
        {
            ParseStatement(
                $$"""
                  var {{toName}} = {{ctor}}
                  {
                  {{string.Join("\n", matches.Select(x => MapOne(x.from, x.to!)))}}
                  };
                  """)
        };
        
        if (mapConfig.Extra is { Length: > 0 })
        {
            statements.AddRange(method.ContainingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => mapConfig.Extra.Contains(x.Name) && (isSelf
                    ? x.Parameters.Length == 1 && x.Parameters[0].Type.Is(to)
                    : x.Parameters.Length == 2 && x.Parameters[0].Type.Is(from) && x.Parameters[1].Type.Is(to) || x.Parameters[1].Type.Is(from) && x.Parameters[0].Type.Is(to)))
                .Select(symbol => ParseStatement(isSelf
                    ? $"this.{symbol.Name}({toName});"
                    : $"{symbol.ContainingType.GetFullyQualifiedName()}.{symbol.Name}({
                        (symbol.Parameters[0].Type.Is(from) ? $"{fromName}, {toName}" : $"{toName}, {fromName}")
                    });")));
        }
        
        return Block(statements.Append(ParseStatement($"return {toName};")));

        

        string MapOne(string fromProperty, string toProperty)
        {
            return $"{toProperty} = {fromName}.{fromProperty},";
        }
    }

    private static string GenerateSpecifiedCtor(
        ITypeSymbol type,
        string fromName,
        string[] specifiedProps) =>
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

            return  $"new {type.GetFullyQualifiedName()}({string.Join(", ", matches)})";
            notfound:
            continue;
        }
        return $"new {type.GetFullyQualifiedName()}()";
    }
    
    private static (
        HashSet<string> fromExcludes,
        HashSet<string> toExcludes,
        HashSet<string> fromIncludes,
        HashSet<string> toIncludes
        ) 
        GetExcludes(ImmutableArray<AttributeData> attributes,
        ISymbol fromType,
        ISymbol toType)
    {
        var fromExcludes = new HashSet<string>();
        var toExcludes   = new HashSet<string>();
        var fromIncludes = new HashSet<string>();
        var toIncludes   = new HashSet<string>();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass!.ToDisplayString() == MapExclude)
            {
                var attr = attribute.ToAttribute<MapExcludeAttribute>();
                var name = attr.Property;
                var type = ((Type)attr.BelongsTo).Symbol;
                if (type.Is(fromType)) fromExcludes.Add(name);
                if (type.Is(toType)) toExcludes.Add(name);
            }

            if (attribute.AttributeClass!.ToDisplayString() == MapInclude)
            {
                var attr = attribute.ToAttribute<MapIncludeAttribute>();
                var name = attr.Property;
                var type = ((Type)attr.BelongsTo).Symbol;
                if (type.Is(fromType)) fromIncludes.Add(name);
                if (type.Is(toType)) toIncludes.Add(name);
            }
        }

        return (fromExcludes, toExcludes, fromIncludes, toIncludes);
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

    /// <summary>
    /// 获取某方法对类型的访问权
    /// </summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    private static Accessibility GetAccess(IMethodSymbol method, ITypeSymbol type)
    {
        var ret                                                        = Accessibility.Public;
        if (method.ContainingAssembly.Is(type.ContainingAssembly)) ret |= Accessibility.Internal;
        if (type.TypeKind == TypeKind.Interface) return ret;
        var @class = method.ContainingType;
        if (@class.Is(type))
        {
            ret |= Accessibility.Protected | Accessibility.Private;
        }
        else
        {
            while (@class.BaseType != null)
            {
                @class = @class.BaseType;
                if (!@class.Is(type)) continue;
                ret |= Accessibility.Protected;
                break;
            }
        }

        return ret;
    }

    private static bool Compatible(string one, string another)
    {
        return string.Equals(
            one.Replace("_", ""),
            another.Replace("_", ""),
            StringComparison.OrdinalIgnoreCase);
    }
}
