using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Antelcat.AutoGen.SourceGenerators.Generators;
using Microsoft.CodeAnalysis.CSharp;

namespace Antelcat.AutoGen.Tests;

public class SampleSourceGeneratorTests
{

    [Test]
    public void TestGenerator()
    {
        // Create an instance of the source generator.
        var generator = new StringToExtensionGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // To run generators, we can use an empty compilation.
        var compilation = CSharpCompilation.Create(nameof(SampleSourceGeneratorTests));

        // Run generators. Don't forget to use the new compilation rather than the previous one.
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        /*// Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .ToArray();

        // In this case, it is enough to check the file name.
        Assert.Equivalent(new[]
        {
            "User.g.cs",
            "Document.g.cs",
            "Customer.g.cs"
        }, generatedFiles);*/
    }

    [Test]
    public void TestSerialize()
    {
        var converter = TypeDescriptor.GetConverter(typeof(IEnumerable<int>));
        var res       = converter.ConvertToString(null, null, new List<int> { 1, 2, 3, 4 });
        var guid      = converter.ConvertFromString(res);
        var obj = new SomeClass
        {
            Name = "Test",
            Guid = Guid.NewGuid(),
            Version = new Version(1,1,1,1)
        };
        var json = JsonSerializer.Serialize(obj);
        var obj2 = JsonSerializer.Deserialize<SomeClass>(json);
        Debugger.Break();
    }

    public record SomeClass()
    {
        public string Name { get; set; }
        
        public Guid Guid { get; set; }

        public Version Version { get; set; }
    }
}