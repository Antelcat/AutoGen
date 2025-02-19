using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Antelcat.AutoGen.AssemblyWeavers.Tests;

public class Tests
{
    private static FilePath Here([CallerFilePath] string? here = null) => here ?? AppContext.BaseDirectory;

    private static FilePath Root => (Here() << 2) / "Antelcat.AutoGen.Native" / "bin" / "Debug";

    private static readonly string Core = Root / "net8.0" / "Antelcat.AutoGen.Native.dll";

    private static readonly string Framework = Root / "net462" / "Antelcat.AutoGen.Native.exe";

    private static readonly string Standard = Root / "netstandard2.0" / "Antelcat.AutoGen.Native.dll";

    private class TestLogger : TaskLogger
    {
        public override void LogDebug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }

        public override void LogWarning(string message)
        {
            Console.WriteLine($"WARNING: {message}");
        }

        public override void LogError(string message)
        {
            Debugger.Break();
            Console.Error.WriteLine($"ERROR: {message}");
        }
    }

    [SetUp]
    public void Setup()
    {
    }

#if NET

    [Test]
    public void TestCore() =>
        WeaveTaskInternal.Execute(new TestWaveArgument
        {
            AssemblyFile = Core,
            References = ""

        }, new TestLogger(), CancellationToken.None);

    [Test]
    public void TestStandard() =>
        WeaveTaskInternal.Execute(new TestWaveArgument
        {
            AssemblyFile = Standard,
            References = ""

        },  new TestLogger(), CancellationToken.None);
#else
    [Test]
    public void TestFramework() =>
        WeaveTaskInternal.Execute(new TestWaveArgument
        {
            AssemblyFile = Framework,
            References = ""
        },  new TestLogger(), CancellationToken.None);
#endif
}

public class TestWaveArgument : IWaveArguments
{
    public required string  AssemblyFile              { get; set; }
    public          string? AssemblyOriginatorKeyFile { get; set; }
    public          bool    SignAssembly              { get; set; }
    public          bool    DelaySign                 { get; set; }
    public          bool    ReadWritePdb              { get; set; }
    public          string? IntermediateDirectory     { get; set; }
    public          string  References                { get; set; }
}