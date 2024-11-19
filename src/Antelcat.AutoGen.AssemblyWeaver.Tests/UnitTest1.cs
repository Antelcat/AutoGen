using System.Runtime.CompilerServices;

namespace Antelcat.AutoGen.AssemblyWeaver.Tests;

public class Tests
{
    private static FilePath Here([CallerFilePath] string? here = null) => here ?? AppContext.BaseDirectory;

    private static readonly string Target = (Here() << 2) / "Antelcat.AutoGen.Native" / "bin" / "Debug" / "net8.0" /
                                            "Antelcat.AutoGen.Native.dll";


    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        WeaveTaskInternal.Execute(Target);
    }
}