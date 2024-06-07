using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Marshal;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Marshal;

[Generator(LanguageNames.CSharp)]
public class UnmanagedArrayGenerator : AttributeDetectBaseGenerator<AutoUnmanagedArray>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var syntaxContext in syntaxArray)
        {
            if (syntaxContext.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            var unmanagedArray = syntaxContext.GetAttributes<AutoUnmanagedArray>().First();
            var type           = unmanagedArray.UnmanagedType?.QualifiedFullName() ?? unmanagedArray.TemplateType;
            if (type is null) continue;
            var size = unmanagedArray.Size;
            var names = Enumerable.Range(0, size)
                .Select(static x => $"item{x}")
                .ToArray();
            var members = names
                .Select(x => $"{unmanagedArray.Accessibility.ToCodeString()} {type} {x};")
                .Select(static x => ParseMemberDeclaration(x)!)
                .ToArray();
            var enumerator = ParseMemberDeclaration(
                $$"""
                  public global::System.Collections.Generic.IEnumerator<{{type}}> Enumerate() {
                      {{string.Join("\n", names.Select(x => $"yield return {x};").ToArray())}}
                  }
                  """
            )!;
            var unit     = CompilationUnit().AddPartialType(typeSymbol, 
                x => x.AddMembers([..members, enumerator]));
            var fileName = typeSymbol.ToType().QualifiedFullFileName();
            var source   = unit.NormalizeWhitespace().ToFullString();
            context.AddSource(
                $"AutoKeyAccessor__{fileName}.g.cs",
                SourceText(source));
        }
    }
}