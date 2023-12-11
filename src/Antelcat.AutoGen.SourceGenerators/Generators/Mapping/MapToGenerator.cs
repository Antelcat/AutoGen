using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping;

[Generator(LanguageNames.CSharp)]
public class MapToGenerator : IIncrementalGenerator
{
    private static readonly string AutoMap    = $"{typeof(GenerateMapAttribute).FullName}";
    private static readonly string MapBetween = $"{typeof(MapBetweenAttribute).FullName}";
    private static readonly string MapIgnore  = $"{typeof(MapIgnoreAttribute).FullName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "",
            static (node, t) => node is MethodDeclarationSyntax,
            static (ctx, t) => ctx);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()), 
            static (ctx, t) =>
        {
            foreach (var syntax in t.Right)
            {
                if (syntax.TargetSymbol is not ITypeSymbol @class) continue;
                if (@class.IsStatic)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.Error.AM0001(nameof(MapToGenerator)),
                        syntax.TargetNode.GetLocation(),
                        nameof(GenerateMapAttribute), "[static] keyword"));
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
                             x.AttributeClass?.ToDisplayString() == AutoMap))
                {
                    var attr = attribute.ToAttribute<GenerateMapAttribute>();
                    var type = attribute.ConstructorArguments[0].Value;
                    if (type is not INamedTypeSymbol typeSymbol) continue;
                    if (typeSymbol.IsStatic)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.Error.AM0001(nameof(MapToGenerator)),
                            syntax.TargetNode.GetLocation(),
                            nameof(GenerateMapAttribute), "[static] keyword"));
                        continue;
                    }
                    if (generated.Contains(typeSymbol, SymbolEqualityComparer.Default)) continue; //repeated
                    generated.Add(typeSymbol);


                    var mapperName = "To" + typeSymbol.MetadataName;
                       

                    var allowInternal = SymbolEqualityComparer.Default.Equals(
                        typeSymbol.ContainingAssembly,
                        @class.ContainingAssembly
                    );

                    var targetMembers = typeSymbol
                        .GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(x => IsValidProperty(x, allowInternal, attr.ExportTo))
                        .ToList();


                    var method = MethodDeclaration(ParseTypeName(typeSymbol.GetFullyQualifiedName()), mapperName)
                        .WithModifiers(SyntaxTokenList.Create(Token(typeSymbol.DeclaredAccessibility
                            .GetSyntaxKind()
                            .First()))).AddGenerateAttribute(typeof(MapToGenerator));
                    var canNew = CanNew(typeSymbol);

                    const string argName = "target";

                    var targetFullName = typeSymbol.GetFullyQualifiedName();
                    method = method.WithParameterList(
                        canNew
                            ? ParameterList()
                            : ParseParameterList($"({targetFullName}  {argName})"));

                    var extra = new List<StatementSyntax>();
                    if (attr.Extra is { Length: > 0 })
                    {
                        extra.AddRange(
                            from m in attr.Extra.Where(static x => !string.IsNullOrWhiteSpace(x))
                            where extraMethods.Any(x =>
                                x.Name == m && SymbolEqualityComparer.Default.Equals(x.Parameters[0].Type, @class))
                            select ParseStatement($"{m}({argName});"));
                    }

                    partial = partial.AddMembers(method.WithBody(Block(
                        ParseStatement(
                            canNew
                                ? $$"""
                                    var {{argName}} = new {{targetFullName}}()
                                    {
                                        {{string.Join(",\n", MapProp(targetMembers, availableMembers
                                            .Where(x => x.property.DeclaredAccessibility.IsIncludedIn(attr.ExportFrom))
                                            .ToList()!, typeSymbol))}}
                                    };
                                    """
                                : "")
                    )).AddBodyStatements(extra.ToArray())
                    .AddBodyStatements(ParseStatement($"return {argName};")));
                }


                var unit = CompilationUnit()
                    .AddMembers(
                        NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
                            .AddMembers(partial));

                ctx.AddSource($"{@class.Name}.g.cs", unit.NormalizeWhitespace().GetText(Encoding.UTF8));
            }
        });
    }

    private static bool Match(string name, string targetName)
    {
        return string.Equals(
            name.Replace("_", ""),
            targetName.Replace("_", ""), StringComparison.OrdinalIgnoreCase);
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
            ? symbol.DeclaredAccessibility is
                Accessibility.Internal
                or Accessibility.Public
                or Accessibility.ProtectedOrInternal
            : symbol.DeclaredAccessibility == Accessibility.Public;
    }


    private static bool CanNew(INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Class && symbol.Constructors.Any(static x => x.Parameters.Length == 0);

    private static IEnumerable<string> MapProp(
        IReadOnlyCollection<IPropertySymbol> targetProps,
        IEnumerable<(IPropertySymbol property, List<MapConfig> configs)> thisProps,
        INamedTypeSymbol targetType)
    {
        foreach (var thisProp in thisProps)
        {
            IPropertySymbol? targetProp = null;
            var              ignored    = false;
            var              certain    = false;
            if (thisProp.configs.Count > 0)
            {
                foreach (var config in thisProp.configs)
                {
                    if (config.IsIgnore)
                    {
                        if (config.Types           != null
                            && config.Types.Length != 0
                            && !config.Types!.Contains(targetType, SymbolEqualityComparer.Default))
                            continue;
                        ignored = true;
                        break;
                    }

                    if (config.Type is null)
                    {
                        if (certain) continue;
                        targetProp = targetProps.FirstOrDefault(x => Match(x.Name, config.Name!));
                        continue;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(config.Type, targetType)) continue;
                    targetProp = targetProps.FirstOrDefault(x => Match(x.Name, config.Name!));
                    certain    = true;
                }
            }
            if(ignored) continue;
            targetProp ??= targetProps.FirstOrDefault(x => Match(x.Name, thisProp.property.Name));

            if (targetProp == null)
            {
                continue;
            }
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
            if (name == MapBetween)
            {
                var nameArg = attribute.ConstructorArguments[0].GetArgumentString();
                if (string.IsNullOrWhiteSpace(nameArg)) return null;
                return new MapConfig
                {
                    IsIgnore = false,
                    Name     = nameArg,
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