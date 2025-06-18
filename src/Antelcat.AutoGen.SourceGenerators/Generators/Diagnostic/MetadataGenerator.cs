using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class MetadataGenerator : AttributeDetectBaseGenerator<MetadataScript>
{
    private static readonly ScriptOptions options = ScriptOptions.Default
        .WithReferences(AppDomain
            .CurrentDomain
            .GetAssemblies()
            .OrderBy(x => x.FullName)
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)));
    
   

    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var cancel = contexts.SourceProductionContext.CancellationToken;
        var tasks  = new List<Task>();
        var count = 0;
        foreach (var syntaxContext in contexts.SyntaxContexts)
        {
            if (syntaxContext.TargetNode is not MethodDeclarationSyntax { Body.Statements: var statements }) continue;
            var body = string.Join("\n", statements.Select(x => x.GetText()));
            var usings = syntaxContext.TargetNode.SyntaxTree.GetCompilationUnitRoot()
                .Usings.Select(x=>x.Name.ToString()).ToArray();
            var script = CSharpScript.Create<object?>(body, options.AddImports(usings),
            typeof(Lazy<object[]>));
            foreach (var attribute in syntaxContext.Attributes)
            {
                var name    = $"MetadataScript_{count++}.g.cs";
                var attr    = attribute.ToAttribute<MetadataScript>();
                var globals = new Lazy<object[]>(() => attr.Args);
                if (cancel.IsCancellationRequested) return;
                tasks.Add(RunAsync());
                continue;
                
                async Task RunAsync()
                {
                    try
                    {
                        var result = await script.RunAsync(globals, cancel);
                        var retVal = result.ReturnValue;
                        if (cancel.IsCancellationRequested || retVal is null) return;
                        contexts.SourceProductionContext.AddSource(name,
                            ParseCompilationUnit(retVal.ToString()).NormalizeWhitespace().GetText(Encoding.UTF8));
                    }
                    catch(Exception ex)
                    {
                        contexts.SourceProductionContext.AddSource(name,
                            SourceText.From(ex.ToString(), Encoding.UTF8));
                    }
                }
            }
        }

        Task.WaitAll(tasks.ToArray(), cancel);
    }
}