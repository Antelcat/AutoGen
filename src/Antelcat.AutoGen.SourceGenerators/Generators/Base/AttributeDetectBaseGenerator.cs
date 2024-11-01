using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Base;

public abstract class AttributeDetectBaseGenerator<TAttribute> : IIncrementalGenerator where TAttribute : Attribute
{
    protected string AttributeName { get; } = typeof(TAttribute).FullName!;
    protected abstract bool FilterSyntax(SyntaxNode node);
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            (node, _) => FilterSyntax(node),
            (ctx, _) => ctx
        );

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()), 
            (ctx, tuple) =>
        {
            try
            {
                Initialize(new IncrementalGeneratorContexts(context, ctx, tuple.Left, tuple.Right));
            }
            catch (Exception e)
            {
                //
            }
        });
    }

    protected record IncrementalGeneratorContexts(
        IncrementalGeneratorInitializationContext InitializationContext,
        SourceProductionContext SourceProductionContext,
        Compilation Compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> SyntaxContexts);

    protected abstract void Initialize(IncrementalGeneratorContexts contexts);
}