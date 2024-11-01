using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class KeyEnumerableGenerator: AttributeDetectBaseGenerator<AutoKeyEnumerableAttribute>
{
    protected override bool   FilterSyntax(SyntaxNode node) => true;
    private readonly   string IEnumerableString = "global::System.Collections.Generic.IEnumerable<string>";

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, _, syntaxArray) = contexts;
        foreach (var syntaxContext in syntaxArray)
        {
            if (syntaxContext.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            var keyEnumerable = syntaxContext.GetAttributes<AutoKeyEnumerableAttribute>().First();
            var members = (keyEnumerable.IncludeInherited ? typeSymbol.GetAllMembers() : typeSymbol.GetMembers())
                .ToList();
            IList<string> keys = [];

            if (keyEnumerable.MemberTypes.HasFlag(MemberTypes.Property))
            {
                foreach (var symbol in members.OfType<IPropertySymbol>())
                {
                    if (!Filter(symbol, out _)) continue;
                    if (keys.Contains(symbol.MetadataName)) continue;
                    keys.Add(symbol.MetadataName);
                }
            }

            if (keyEnumerable.MemberTypes.HasFlag(MemberTypes.Field))
            {
                foreach (var symbol in members.OfType<IFieldSymbol>()
                             .Where(static x => !x.IsImplicitlyDeclared))
                {
                    if (!Filter(symbol, out _)) continue;
                    if (keys.Contains(symbol.Name)) continue;
                    keys.Add(symbol.Name);
                }
            }

            var ignores = keyEnumerable.Ignores ?? [];
            foreach (var ignore in ignores) keys.Remove(ignore);
            var body           = string.Join("\n", keys.Select(x => $"yield return nameof({x});").ToArray());
            var generateMethod = keyEnumerable.GenerateType is not MemberTypes.Property;
            var method =
                $$"""
                  {{keyEnumerable.Accessibility.ToCodeString()}} {{IEnumerableString}} {{keyEnumerable.Name}}{{
                      (generateMethod ? "()" : "")
                  }} {
                     {{(generateMethod
                         ? body
                         : $$"""
                             get {
                             {{body}}
                             }
                             """)}}
                  }
                  """;
            var className = typeSymbol.Name;

            var nameSpace = typeSymbol.ToType().Namespace;
            nameSpace = nameSpace is null ? "" : $"{nameSpace}.";
            var unit = CompilationUnit().AddPartialType(typeSymbol, x => x.AddMembers(ParseMemberDeclaration(method)!));
            context.AddSource($"AutoKeyEnumerable__{nameSpace}{className.ToQualifiedFileName()}.g.cs",
                SourceText(unit.NormalizeWhitespace().ToFullString()));
            continue;

            bool Filter(ISymbol symbol, out bool isSelf)
            {
                isSelf = SymbolEqualityComparer.Default.Equals(symbol.ContainingType, typeSymbol);
                if (symbol.IsStatic) return false;
                return (isSelf || symbol.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Private) &&
                       (isSelf || SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly,
                            typeSymbol.ContainingAssembly) ||
                        symbol.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Internal);
            }
        }
    }
}