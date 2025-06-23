using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Feast.CodeAnalysis;
using Feast.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class MetadataGenerator : AttributeDetectBaseGenerator<MetadataScriptAttribute>
{
    private static readonly ScriptOptions options = ScriptOptions.Default
        .WithReferences(AppDomain
            .CurrentDomain
            .GetAssemblies()
            .OrderBy(x => x.FullName)
            .Where(x => !x.IsDynamic && !string.IsNullOrEmpty(x.Location)))
        .WithImports("System", "System.Linq");

    protected override bool FilterSyntax(SyntaxNode node) 
    {
        return true;
    }

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var cancel = contexts.SourceProductionContext.CancellationToken;
        var tasks  = new List<Task>();
        var count  = 0;
        foreach (var syntaxContext in contexts.SyntaxContexts)
        {
            if (syntaxContext.TargetNode is not MethodDeclarationSyntax { Body.Statements: var statements } method) continue;
            var alloc =
                method.ParameterList.Parameters.Count switch
                {
                    0 => [],
                    1 => method.ParameterList.Parameters.First() is { } param &&
                         param.Type.FullName(syntaxContext.SemanticModel) is "object[]"
                        ? [$"var {param.Identifier.Text} = Value;"]
                        : Map(),
                    _ => Map()
                };

            var body = string.Join("\n",
                statements.Select(x => x.GetText().ToString())
                    .Prepend(string.Join("\n", alloc)));
            var usings = syntaxContext.TargetNode.SyntaxTree.GetCompilationUnitRoot()
                .Usings.Select(x=>x.Name.ToString()).ToArray();
            var container = syntaxContext.TargetSymbol.ContainingType;
           
            Feast.CodeAnalysis.Scripting.Script<object?> script;
            try
            {
                script = CSharpScript.Create<object?>(body, options.AddImports(usings),
                    typeof(Lazy<object[]>));
            }
            catch(Exception ex)
            {
                contexts.SourceProductionContext
                    .AddSource(container.MetadataName
                            .ToQualifiedFileName(nameof(MetadataScriptAttribute), "CompileError"),
                        SourceText.From(ex.ToString(), Encoding.UTF8));
                continue;
            }
            
            
            foreach (var attribute in syntaxContext.Attributes)
            {
                try
                {
                    var attr = attribute.ToAttribute<MetadataScriptAttribute>();
                    var name = attr.FileName ?? (container.MetadataName + "." + method.Identifier.Text)
                        .ToQualifiedFileName(nameof(MetadataScriptAttribute), count++.ToString());
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
                                ParseCompilationUnit(retVal switch
                                {
                                    IEnumerable<object> enumerable => string.Join("\n",
                                        enumerable.Select(x => x?.ToString() ?? string.Empty)),
                                    string str => str,
                                    _          => retVal.ToString()
                                }).NormalizeWhitespace().GetText(Encoding.UTF8));
                        }
                        catch (Exception ex)
                        {
                            contexts.SourceProductionContext.AddSource(name,
                                SourceText.From(ex.ToString(), Encoding.UTF8));
                        }
                    }
                }
                catch (Exception ex)
                {
                    contexts.SourceProductionContext.AddSource("Error" + Guid.NewGuid(),
                        SourceText.From(ex.ToString(), Encoding.UTF8));

                }
            }

            continue;

            IEnumerable<string> Map() =>
                method.ParameterList.Parameters.Select((x, i) =>
                    $"var {x.Identifier.Text} = ({x.Type.FullName(syntaxContext.SemanticModel)})Value[{i}];");
        }

        Task.WaitAll(tasks.ToArray(), cancel);
    }
}