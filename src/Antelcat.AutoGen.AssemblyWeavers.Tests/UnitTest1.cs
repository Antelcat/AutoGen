using System.Runtime.CompilerServices;

namespace Antelcat.AutoGen.AssemblyWeavers.Tests;

public class Tests
{
    private static FilePath Here([CallerFilePath] string? here = null) => here ?? AppContext.BaseDirectory;

    private static FilePath Root => (Here() << 2) / "Antelcat.AutoGen.Native" / "bin" / "Debug";

    private static readonly string Core = Root / "net8.0" / "Antelcat.AutoGen.Native.dll";

    private static readonly string Framework = Root / "net462" / "Antelcat.AutoGen.Native.exe";

    private static readonly string Standard = Root / "netstandard2.0" / "Antelcat.AutoGen.Native.dll";


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
        }, x => throw new Exception(x));

    [Test]
    public void TestStandard() =>
        WeaveTaskInternal.Execute(new TestWaveArgument
        {
            AssemblyFile = Standard,
        }, x => throw new Exception(x));
#else
    [Test]
    public void TestFramework() =>
        WeaveTaskInternal.Execute(new TestWaveArgument
        {
            AssemblyFile = Framework,
        }, x => throw new Exception(x));
#endif
}

public class TestWaveArgument : IWaveArguments
{
    public required string  AssemblyFile              { get; set; }
    public          string? AssemblyOriginatorKeyFile { get; set; }
    public          bool    SignAssembly              { get; set; }
    public          bool    DelaySign                 { get; set; }
    public          bool    ReadWritePdb              { get; set; }
}