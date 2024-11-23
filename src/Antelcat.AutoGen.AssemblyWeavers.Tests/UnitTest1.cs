using System.Runtime.CompilerServices;

namespace Antelcat.AutoGen.AssemblyWeavers.Tests;

public class Tests
{
    private static FilePath Here([CallerFilePath] string? here = null) => here ?? AppContext.BaseDirectory;

    private static readonly string Native =
        (Here() << 2) / "Antelcat.AutoGen.Native" / "bin" / "Debug" / "net8.0" /
        "Antelcat.AutoGen.Native.dll";

    private static readonly string Framework4 =
        (Here() << 2) / "Antelcat.AutoGen.Framework" / "bin" / "Debug" / "net40" /
        "Antelcat.AutoGen.Framework.exe";

    [SetUp]
    public void Setup()
    {
    }

#if !NET
    [Test]
    public void TestFramework()
    {
        WeaveTaskInternal.Execute(new WeaveTask
        {
            AssemblyFile = Framework4
        });
    }
#else
    [Test]
    public void TestNative()
    {
        WeaveTaskInternal.Execute(new WeaveTask
        {
            AssemblyFile = Native
        });
    }
#endif
}