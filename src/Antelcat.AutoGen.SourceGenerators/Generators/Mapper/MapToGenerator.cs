using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Entity;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapper;

[Generator(LanguageNames.CSharp)]
public class MapToGenerator : IIncrementalGenerator
{
    private static readonly string GenerateMapTo = $"{typeof(GenerateMapToAttribute).FullName}";
    private static readonly string MapToName     = $"{typeof(MapToNameAttribute).FullName}";
    private static readonly string MapIgnore     = $"{typeof(MapIgnoreAttribute).FullName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateMapTo,
            static (node, t) => node is ClassDeclarationSyntax,
            static (ctx, t) => ctx);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()), static (ctx, t) =>
        {
            foreach (var syntax in t.Right)
            {
                if (syntax.TargetSymbol is not ITypeSymbol @class) continue;
                if (@class.IsStatic)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.Error.AA0001(nameof(MapToGenerator)),
                        syntax.TargetNode.GetLocation(),
                        nameof(GenerateMapToAttribute), "[static] keyword"));
                    continue;
                }


                var availableMembers = @class.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(static x => !x.IsWriteOnly && !x.IsStatic)
                    .Select(static x =>
                        (
                            property: x,
                            configs: x
                                .GetAttributes()
                                .Select(MapConfig.FromAttributeData)
                                .Where(static c => c != null)
                                .ToList()
                        )
                    )
                    .ToList();

                var extraMethods = @class.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(static x => x.Parameters.Length == 1)
                    .ToList();

                var partial = ClassDeclaration(@class.Name)
                    .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PartialKeyword)));


                var generated = new List<ITypeSymbol>();
                foreach (var attribute in syntax.Attributes.Where(static x =>
                             x.AttributeClass?.ToDisplayString() == GenerateMapTo))
                {
                    var attr = attribute.ToAttribute<GenerateMapToAttribute>();
                    var type = attribute.ConstructorArguments[0].Value;
                    if (type is not INamedTypeSymbol typeSymbol) continue;
                    if (generated.Contains(typeSymbol, SymbolEqualityComparer.Default)) continue; //repeated
                    generated.Add(typeSymbol);


                    var mapperName = string.IsNullOrWhiteSpace(attr.Alias)
                        ? "To" + typeSymbol.MetadataName
                        : attr.Alias!;

                    var allowInternal = SymbolEqualityComparer.Default.Equals(
                        typeSymbol.ContainingAssembly,
                        @class.ContainingAssembly
                    );

                    var targetMembers = typeSymbol
                        .GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(x => IsValidProperty(x, allowInternal, attr.TargetAccessibility))
                        .ToList();


                    var method = MethodDeclaration(ParseTypeName(typeSymbol.GetFullyQualifiedName()), mapperName)
                        .WithModifiers(SyntaxTokenList.Create(Token(typeSymbol.DeclaredAccessibility.GetSyntaxKind().First())));
                    var canNew = CanNew(typeSymbol);

                    const string argName = "target";

                    var targetFullName = typeSymbol.GetFullyQualifiedName();
                    method = method.WithParameterList(
                        canNew
                            ? ParameterList()
                            : ParseParameterList($"({targetFullName}  {argName})"));

                    var extra = string.Empty;
                    if (attr.Extra is { Length: > 0 })
                    {
                        
                    }

                    partial = partial.AddMembers(method.WithBody(Block(
                        ParseStatement(
                            canNew
                                ? $$"""
                                    var {{argName}} = new {{targetFullName}}()
                                    {
                                        {{string.Join(",\n", MapProp(targetMembers, availableMembers
                                            .Where(x => IsValidAccessibility(x.property.DeclaredAccessibility, attr.Accessibility))
                                            .ToList()!, typeSymbol))}}
                                    };
                                    """
                                : ""),
                        ParseStatement($"return {argName};")
                    )));
                }


                var unit = CompilationUnit()
                    .AddMembers(
                        NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
                            .AddMembers(partial));

                ctx.AddSource($"{@class.Name}.g.cs", unit.NormalizeWhitespace().GetText(Encoding.UTF8));
            }
        });
    }

    private static bool Match(string name, string? targetName)
    {
        return string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidProperty(IPropertySymbol symbol, 
        bool allowInternal,
        ComponentModel.Accessibility accessibility)
    {
        return symbol is
               {
                   IsReadOnly: false,
                   IsStatic  : false,
               }
               && allowInternal && accessibility.HasFlag(ComponentModel.Accessibility.Internal)
            ? symbol.DeclaredAccessibility is Accessibility.Internal or Accessibility.Public
            : symbol.DeclaredAccessibility == Accessibility.Public;
    }

    private static bool IsValidAccessibility(Accessibility accessibility,
        ComponentModel.Accessibility targetAccessibility)

        => accessibility switch
        {
            Accessibility.Public    => targetAccessibility.HasFlag(ComponentModel.Accessibility.Public),
            Accessibility.Private   => targetAccessibility.HasFlag(ComponentModel.Accessibility.Private),
            Accessibility.Internal  => targetAccessibility.HasFlag(ComponentModel.Accessibility.Internal),
            Accessibility.Protected => targetAccessibility.HasFlag(ComponentModel.Accessibility.Protected),
            Accessibility.ProtectedOrInternal =>
                targetAccessibility.HasFlag(ComponentModel.Accessibility.Protected) &&
                targetAccessibility.HasFlag(ComponentModel.Accessibility.Internal),
            _ => false
        };

    private static bool CanNew(INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Class && symbol.Constructors.Any(static x => x.Parameters.Length == 0);

    private static IEnumerable<string> MapProp(
        IReadOnlyCollection<IPropertySymbol> targetProps,
        IEnumerable<(IPropertySymbol property, List<MapConfig> configs)> thisProps,
        INamedTypeSymbol targetType)
    {
        foreach (var thisProp in thisProps)
        {
            string? candidate = null;
            var     ignored   = false;
            var     certain   = false;
            if (thisProp.configs.Count > 0)
            {
                foreach (var config in thisProp.configs)
                {
                    if (config.IsIgnore)
                    {
                        if (config.Types == null 
                            || config.Types.Length == 0
                            || config.Types!.Contains(targetType, SymbolEqualityComparer.Default))
                        {
                            ignored = true;
                            break;
                        }
                    }
                    else
                    {
                        if (config.Type is null)
                        {
                            if(certain) continue;
                            if (targetProps.All(x => Match(x.Name, config.Name))) continue;
                            candidate = config.Name;
                            continue;
                        }

                        if (!SymbolEqualityComparer.Default.Equals(config.Type, targetType)) continue;
                        if (targetProps.All(x => Match(x.Name, config.Name))) continue;
                        candidate = config.Name;
                        certain   = true;
                        break;
                    }
                }
            }
            if(ignored) continue;
            
            if (candidate != null)
            {
                yield return $"{candidate} = this.{thisProp.property.Name}";
                continue;
            }

            var targetProp = targetProps.FirstOrDefault(x => 
                Match(x.Name, thisProp.property.Name));
            if (targetProp == null) continue;
            yield return $"{targetProp.Name} = this.{thisProp.property.Name}";
        }
    }

    private class MapConfig
    {
        public string?           Name     { get; private init; }
        public INamedTypeSymbol? Type     { get; private init; }
        public bool              IsIgnore { get; private init; }
        public INamedTypeSymbol?[]? Types { get; private init; } = Array.Empty<INamedTypeSymbol>();

        public static MapConfig? FromAttributeData(AttributeData attribute)
        {
            var name = attribute.AttributeClass!.ToDisplayString();
            if (name == MapToName)
            {
                return new MapConfig
                {
                    IsIgnore = false,
                    Name     = attribute.ConstructorArguments[0].GetArgumentString()!,
                    Type = attribute.NamedArguments.Length == 0
                        ? null
                        : attribute.NamedArguments.First().Value.Value as INamedTypeSymbol
                };
            }

            if (name == MapIgnore)
            {
                return new MapConfig
                {
                    IsIgnore = true,
                    Types    = attribute.ConstructorArguments[0].GetArgumentArray<INamedTypeSymbol>()
                };
            }

            return null;
        }
    }
}