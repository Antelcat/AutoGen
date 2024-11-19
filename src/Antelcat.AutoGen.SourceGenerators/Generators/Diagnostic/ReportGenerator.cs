using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator(LanguageNames.CSharp)]
internal class IncrementReportGenerator : AttributeDetectBaseGenerator<AutoReportAttribute>
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


[Generator(LanguageNames.CSharp)]
internal class FinalReportGenerator : ISourceGenerator
{
    private class Receiver : ISyntaxReceiver
    {
        private readonly IList<BaseTypeDeclarationSyntax> types = [];
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is BaseTypeDeclarationSyntax typeDeclaration) types.Add(typeDeclaration);
        }

        public IEnumerable<INamedTypeSymbol> Targets(Func<SyntaxTree, SemanticModel> semantic) =>
            types.Select(x => semantic(x.SyntaxTree)
                    .GetDeclaredSymbol(x))
                .OfType<INamedTypeSymbol>()
                .Where(x => x.GetAttributes().GetAttributes<AutoReportAttribute>().Any());
    }
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new Receiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not Receiver receiver) return;
        foreach (var symbol in receiver.Targets(x=>context.Compilation.GetSemanticModel(x)))
        {
            var members = symbol.GetAllMembers().Select(x => $"// {x}");
            context.AddSource(
                symbol.GetFullyQualifiedName().ToQualifiedFileName("AutoReport"),
                SourceText.From(
                    $"""
                     // {symbol.GetFullyQualifiedName()}
                     {string.Join("\n", members)}
                     """, Encoding.UTF8));
        }
    }
}