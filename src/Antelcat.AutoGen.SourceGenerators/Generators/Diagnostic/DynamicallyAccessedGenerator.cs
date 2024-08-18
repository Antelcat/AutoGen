using System;
using System.Collections.Immutable;
using Antelcat.AutoGen.ComponentModel.Abstractions;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen
{
    namespace ComponentModel.Diagnostic
    {
        public sealed class AutoDynamicallyAccessedMembersAttribute(object arg) : AutoGenAttribute
        {
            public object Arg => arg;
        }
    }
    
    namespace SourceGenerators.Generators.Diagnostic
    {
        public class DynamicallyAccessedGenerator : AttributeDetectBaseGenerator<AutoDynamicallyAccessedMembersAttribute>
        {
            protected override bool FilterSyntax(SyntaxNode node) => true;

            protected override void Initialize(SourceProductionContext context, Compilation compilation, ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
            {
                throw new NotImplementedException();
            }
        }
    }
}
