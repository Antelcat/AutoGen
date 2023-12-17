using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.SourceGenerators.Generators;
using Antelcat.AutoGen.SourceGenerators.Generators.Mapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Antelcat.AutoGen.Tests;

public class SampleIncrementalSourceGeneratorTests
{
    [SetUp]
    public void Setup()
    {
        
    }
    
    [Test]
    public void Test()
    {
        var sourceDir = Path.GetFullPath("../../../../");
        var file      = Path.Combine(sourceDir, @"Antelcat.AutoGen.Sample\Examples.cs");
        // Create an instance of the source generator.
        var generator = new MapExtensionGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(SampleSourceGeneratorTests),
            new[]
            {
                CSharpSyntaxTree.ParseText(File.ReadAllText(file))
            },
            new[]
            {
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MapConstructorAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(@"C:\Users\13532\.nuget\packages\netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll")
            });

        // Run generators and retrieve all results.
        driver.RunGenerators(compilation).GetRunResult();

        compilation.GetDiagnostics().Select(x => 
                x.Severity == DiagnosticSeverity.Error 
                    ? x.ToString() 
                    : null)
            .Where(x => x != null)
            .ToList()
            .ForEach(Console.WriteLine);
        
        // All generated files can be found in 'RunResults.GeneratedTrees'.
        //var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Vector3.g.cs"));
    }

    [Test]
    public void TestConverter()
    {
        var converter = new StringConverter();
        var result    = converter.CanConvertTo(typeof(int));
        Debugger.Break();
    }
}