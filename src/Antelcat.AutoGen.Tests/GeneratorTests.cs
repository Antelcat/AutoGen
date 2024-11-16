using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Abstractions;
using Antelcat.AutoGen.SourceGenerators.Generators;
using Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Antelcat.AutoGen.Tests;

public class SampleIncrementalSourceGeneratorTests
{
    [SetUp]
    public void Setup()
    {
    }

    private void RunTest<TGenerator>(params string[] sourceInput) where TGenerator : IIncrementalGenerator, new()
    {
        var driver = CSharpGeneratorDriver.Create(new TGenerator());
        var compilation = CSharpCompilation.Create(nameof(SampleIncrementalSourceGeneratorTests),
            sourceInput.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray(),
            [
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AutoGenAttribute).Assembly.Location)
            ]);
        driver.RunGenerators(compilation).GetRunResult();
        compilation.GetDiagnostics()
            .Select(x =>
                x.Severity == DiagnosticSeverity.Error
                    ? x.ToString()
                    : null)
            .Where(x => x != null)
            .ToList()
            .ForEach(Console.WriteLine);
    }

    [Test]
    public void Test()
    {
        string file = (General.Dir().FullPath << 2) / "Antelcat.AutoGen.Sample" / "Models" / "Accessor" /
                      "INeedAccessor.cs";
        RunTest<KeyAccessorGenerator>(File.ReadAllText(file));
    }

    [Test]
    public void TestConverter()
    {
        var converter = new StringConverter();
        var result    = converter.CanConvertTo(typeof(int));
        Debugger.Break();
    }

    [Test]
    public void TestDeconstruct()
    {
        var file = (General.Dir() << 1).FullPath / "Usings.cs";
        RunTest<AutoDeconstructIndexableGenerator>(File.ReadAllText(file));
    }

    [Test]
    public void TestWatch()
    {
        var file = (General.Dir() << 1).FullPath / "Usings.cs";
        RunTest<WatchGenerator>(File.ReadAllText(file));
    }

    [Test]
    public void TestIndex()
    {
        var cs = typeof(ICollection<>)
            .Assembly
            .ExportedTypes
            .Where(static x =>
                x is
                {
                    IsClass                  : true,
                    ContainsGenericParameters: true,
                }
                && x.GetInterfaces().Contains(typeof(ICollection)))
            .ToList()
            .First();
        var props = cs.GetProperties();
    }

    [Test]
    public void TestPath()
    {
        var relative1 = (FilePath)@"d:\A\B\C" >> @"d:\A\D";
        var relative2 = (FilePath)@"d:\A\B\C" << @"d:\A\D";
    }

    [Test]
    public void TestAnonymous()
    {
        string file = (General.Dir().FullPath << 2) / "Antelcat.AutoGen.Sample" / "Models" / "Diagnostics" /
                      "Anonymous.cs";
        RunTest<TypeInferenceGenerator>(File.ReadAllText(file));
    }

    [Test]
    public void TestExtractInterface()
    {
        string file = (General.Dir().FullPath << 2) / "Antelcat.AutoGen.Sample" / "Models" / "WaitingForInterface.cs";
        RunTest<AutoExtractInterfaceGenerator>(File.ReadAllText(file));
    }

    [Test]
    public void TestRecordPlacebo()
    {
        string file = (General.Dir().FullPath << 2) / "Antelcat.AutoGen.Sample" / "Models" / "Diagnostics" / "Records.cs";
        RunTest<RecordPlaceboGenerator>(File.ReadAllText(file));
    }
}