using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public abstract class AttributeDetectBaseGenerator<TAttribute> : IIncrementalGenerator where TAttribute : Attribute
{
    protected string AttributeName { get; } = typeof(TAttribute).FullName!;
    protected abstract bool FilterSyntax(SyntaxNode node);
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            (node, _) => FilterSyntax(node),
            (ctx, t) => ctx
        );

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()), 
            (ctx, tuple) =>
        {
            try
            {
                Initialize(ctx, tuple.Left, tuple.Right);
            }
            catch (Exception e)
            {
                //
            }
        });
    }

    protected abstract void Initialize(SourceProductionContext context, Compilation compilation, ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray);
}