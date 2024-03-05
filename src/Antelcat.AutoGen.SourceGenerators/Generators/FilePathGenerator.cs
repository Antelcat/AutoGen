using System.Collections.Immutable;
using System.Linq;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class FilePathGenerator : AttributeDetectBaseGenerator<AutoFilePathAttribute>
{
    private const      string Class = "FilePath";

    protected override bool FilterSyntax(SyntaxNode node) => node is CompilationUnitSyntax;

    protected override void Initialize(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var syntaxContext in syntaxArray)
        {
            var attr = syntaxContext
                .GetAttributes<AutoFilePathAttribute>()
                .FirstOrDefault(static x => x.Namespace.IsValidNamespace());
            if (attr is null) continue;
            var text = Antelcat.AutoGen.FilePath.Text.Replace(
                $"{nameof(Antelcat)}.{nameof(AutoGen)}.{nameof(SourceGenerators)}", attr.Namespace);
            if (attr.Accessibility is Accessibility.Internal)
            {
                text = text.Replace("public readonly", "internal readonly");
            }

            context.AddSource($"{attr.Namespace}.{Class}.cs", SourceText(text));
        }
    }
}