using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class WatchGenerator : AttributeDetectBaseGenerator<AutoWatchAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(SourceProductionContext context, Compilation compilation, ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    { 
        foreach (var syntaxContext in syntaxArray)
        {
            var symbol  = (syntaxContext.TargetSymbol as INamedTypeSymbol)!;
            var type    = symbol.ToType();
            var name    = type.Name;
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var rs = symbol.GetMembers().OfType<IPropertySymbol>();
            Debugger.Break();
        }
    }
}