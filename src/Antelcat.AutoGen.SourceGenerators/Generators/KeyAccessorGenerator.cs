using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class KeyAccessorGenerator : AttributeDetectBaseGenerator<AutoKeyAccessor>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var syntaxContext in syntaxArray)
        {
            if (syntaxContext.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            var keyAccessor = syntaxContext.GetAttributes<AutoKeyAccessor>().First();
            var members = (keyAccessor.IncludeInherited ? typeSymbol.GetAllMembers() : typeSymbol.GetMembers())
                .ToList();
            Dictionary<string, (bool get, bool set, bool self, ITypeSymbol type)> dict = [];

            foreach (var symbol in members.OfType<IPropertySymbol>())
            {
                if (!Filter(symbol, out var isSelf)) continue;
                if (dict.TryGetValue(symbol.Name, out var tmp) && tmp.self) continue;
                dict[symbol.Name] = (!symbol.IsWriteOnly, !symbol.IsReadOnly && !symbol.IsInitOnly(), isSelf, symbol.Type);
            }

            if (keyAccessor.IncludeField)
            {
                foreach (var symbol in members.OfType<IFieldSymbol>().Where(static x => !x.IsImplicitlyDeclared))
                {
                    if (!Filter(symbol, out var isSelf)) continue;
                    if (dict.TryGetValue(symbol.Name, out var tmp) && tmp.self) continue;
                    dict[symbol.Name] = (true, true, isSelf, symbol.Type);
                }
            }

            var ignores = keyAccessor.Ignores ?? [];
            foreach (var ignore in ignores) dict.Remove(ignore);
            var canGets = dict.Where(x => x.Value.get).ToList();
            var canSets = dict.Where(x => x.Value.set).ToList();
            var method =
                $$"""
                  {{keyAccessor.Accessibility switch {
                      Accessibility.Public              => "public",
                      Accessibility.Private             => "private",
                      Accessibility.Internal            => "internal",
                      Accessibility.Protected           => "protected",
                      Accessibility.ProtectedOrInternal => "protected internal",
                      _                                 => ""
                  }}} object? this[string key] {
                     {{
                         (keyAccessor.Get ?
                             $$"""
                                  get {
                                   switch (key) {
                                       {{string.Join("\n", canGets.Select(x => {
                                           var belong = x.Value.self ? "this" : "base";
                                           return $"case nameof({belong}.{x.Key}): return {belong}.{x.Key};";
                                       }))}}
                                   };
                                   return null;
                               }           
                                             
                               """ : string.Empty)
                     }}
                     {{
                         (keyAccessor.Set ?
                             $$"""
                                  set {
                                      switch (key) {
                                       {{string.Join("\n", canSets.Select(x => {
                                           var belong = x.Value.self ? "this" : "base";
                                           return $"case nameof({belong}.{x.Key}): {belong}.{x.Key} = ({x.Value.type.GlobalName()})value; break;";
                                       }))}}
                                       };
                                   }
                                             
                               """ : string.Empty)
                     }}
                  }
                  """;
            var className = typeSymbol.Name;

            var nameSpace = syntaxContext.TargetSymbol.ContainingNamespace?.ToDisplayString();

            var unit = CompilationUnit().AddPartialType(typeSymbol, x => x.AddMembers(ParseMemberDeclaration(method)!));
            nameSpace = nameSpace != GlobalNamespace ? "." + nameSpace : string.Empty;
            context.AddSource($"AutoKeyAccessor__{nameSpace}.{className.ToQualifiedFileName()}.g.cs",
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