using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator(LanguageNames.CSharp)]
internal class ReportGenerator : AttributeDetectBaseGenerator<AutoReportAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        foreach (var context in contexts.SyntaxContexts)
        {
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) continue;
            var members = typeSymbol.GetAllMembers().Select(x =>
                $"""
                // {x}
                """
            );
            contexts.SourceProductionContext.AddSource(
                typeSymbol.GetFullyQualifiedName().ToQualifiedFileName("AutoReport"),
                SourceText.From(
                    $"""
                     // {typeSymbol.GetFullyQualifiedName()}
                     {string.Join("\n", members)}
                     """, Encoding.UTF8));
        }
    }
}
