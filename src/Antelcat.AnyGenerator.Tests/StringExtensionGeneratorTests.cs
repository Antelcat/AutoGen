using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Antelcat.AnyGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Antelcat.AnyGenerator.Tests;

public class SampleIncrementalSourceGeneratorTests
{
    private const string VectorClassText = @"
[assembly:Antelcat.AnyGenerator.GenerateStringTo(null),
          Antelcat.AnyGenerator.GenerateStringTo]
[assembly:Antelcat.AnyGenerator.GenerateStringTo]
[assembly:Antelcat.AnyGenerator.GenerateStringTo]
[assembly:Antelcat.AnyGenerator.GenerateStringTo,
          Antelcat.AnyGenerator.GenerateStringTo]
";


    [Fact]
    public void GenerateReportMethod()
    {
        // Create an instance of the source generator.
        var generator = new StringToExtensionGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(SampleSourceGeneratorTests),
            new[] { CSharpSyntaxTree.ParseText(VectorClassText) },
            new[]
            {
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        // Run generators and retrieve all results.
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Vector3.g.cs"));
    }

    [Fact]
    public void TestConverter()
    {
        var converter = new StringConverter();
        var result = converter.CanConvertTo(typeof(int));
        Debugger.Break();
    }
}