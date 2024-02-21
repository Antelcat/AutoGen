using System.Collections.Immutable;
using Antelcat.AutoGen.ComponentModel;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

public class ExtendForGenerator : AttributeDetectBaseGenerator<AutoExtendForAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node)
    {
        throw new System.NotImplementedException();
    }

    protected override void Initialize(SourceProductionContext context, 
        Compilation compilation, 
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        throw new System.NotImplementedException();
    }
}