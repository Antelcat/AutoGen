using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping;

[Generator(LanguageNames.CSharp)]
public class MapExtensionGenerator : IIncrementalGenerator
{
    private static readonly string GenerateMap = typeof(GenerateMapAttribute).FullName!;
    private static readonly string MapBetween  = typeof(MapBetweenAttribute).FullName!;
    private static readonly string MapExclude  = typeof(MapExcludeAttribute).FullName!;
    private static readonly string MapInclude  = typeof(MapIncludeAttribute).FullName!;
    private static readonly string MapIgnore   = typeof(MapIgnoreAttribute).FullName!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateMap,
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
                                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                                    location,
                                    nameof(GenerateMapAttribute),
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
                                    Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                                        location,
                                        nameof(GenerateMapAttribute),
                                        "zero or one parameter"
                                    ));
                                continue;
                            case MethodMode.MapSelf:
                                if (@class.IsStatic)
                                {
                                    ctx.ReportDiagnostic(
                                        Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                                            location,
                                            nameof(GenerateMapAttribute),
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
                            .AddGenerateAttribute(typeof(MapExtensionGenerator))
                            .WithSemicolonToken(default)
                            .WithBody(GenerateMethod(method, fromType, toType, mode is MethodMode.MapSelf,
                                method.GetAttributes()
                                    .First(x => x.AttributeClass!.HasFullyQualifiedMetadataName(GenerateMap))
                                    .ToAttribute<GenerateMapAttribute>())));
                    }

                    var unit = CompilationUnit()
                        .AddMembers(
                            NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
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
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                    location,
                    nameof(GenerateMapAttribute),
                    "should be partial definition"
                ));
        }

        if (method.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                    location,
                    nameof(GenerateMapAttribute),
                    "not return void"
                ));
            return MethodMode.Invalid;
        }

        if (method.ReturnType.TypeKind == TypeKind.Interface)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.Error.AM0002(nameof(MapExtensionGenerator)),
                    location,
                    nameof(GenerateMapAttribute),
                    "return actual type"
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
        bool isSelf,
        GenerateMapAttribute mapConfig)
    {
        var fromName = isSelf ? "this" : method.Parameters[0].Name;
        var toName   = isSelf ? "ret" : method.Parameters[0].Name == "ret" ? "retVal" : "ret";

        var fromAccess = GetAccess(method, from);
        var toAccess   = GetAccess(method, to);

        var configs = GetExcludes(method, from, to);

        var fromProps = from.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                !x.IsWriteOnly                                             &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.ExportFrom) &&
                QualifiedProperty(x, fromAccess, configs.fromExcludes, configs.fromIncludes));

        var toProps = to.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                !x.IsReadOnly                                            &&
                x.DeclaredAccessibility.IsIncludedIn(mapConfig.ExportTo) &&
                QualifiedProperty(x, toAccess, configs.toExcludes, configs.toIncludes))
            .ToList();

        var betweens = method.GetAttributes().Select(x =>
                x.AttributeClass!.HasFullyQualifiedMetadataName(MapBetween)
                    ? x.ToAttribute<MapBetweenAttribute>()
                    : null)
            .Where(x => x != null)
            .ToList();


        var matches = fromProps.Select(x =>
            {
                var name   = x.Name;
                var config = betweens.FirstOrDefault(a => a!.FromProperty == name || a.ToProperty == name);
                if (config != null)
                {
                    var another = config.FromProperty == name ? config.ToProperty : config.FromProperty;
                    if (toProps.Any(p => p.Name == another))
                    {
                        return (from: name, to: another);
                    }
                }

                var match = toProps.FirstOrDefault(y => Compatible(name, y.Name));
                return (from: x.Name, to: match?.Name);
            })
            .Where(x => x.to != null)
            .ToArray();

        var main = ParseStatement(
            $$"""
              var {{toName}} = new {{to.GetFullyQualifiedName()}}()
              {
              {{string.Join("\n", matches.Select(x => MapOne(x.from, x.to!)))}}
              };
              """
        );

        return Block(main, ParseStatement($"return {toName};"));

        

        string MapOne(string toProperty, string fromProperty)
        {
            return $"{toProperty} = {fromName}.{fromProperty},";
        }
    }

    private static (
        HashSet<string> fromExcludes,
        HashSet<string> toExcludes,
        HashSet<string> fromIncludes,
        HashSet<string> toIncludes
        ) 
        GetExcludes(ISymbol method,
        ISymbol fromType,
        ISymbol toType)
    {
        var fromExcludes = new HashSet<string>();
        var toExcludes   = new HashSet<string>();
        var fromIncludes = new HashSet<string>();
        var toIncludes   = new HashSet<string>();
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass!.HasFullyQualifiedMetadataName(MapExclude))
            {
                var name = attribute.ConstructorArguments[0].GetArgumentString()!;
                var type = attribute.ConstructorArguments[1].GetArgumentType()!;
                if (type.Is(fromType)) fromExcludes.Add(name);
                if (type.Is(toType)) toExcludes.Add(name);
            }

            if (attribute.AttributeClass!.HasFullyQualifiedMetadataName(MapInclude))
            {
                var name = attribute.ConstructorArguments[0].GetArgumentString()!;
                var type = attribute.ConstructorArguments[1].GetArgumentType()!;
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