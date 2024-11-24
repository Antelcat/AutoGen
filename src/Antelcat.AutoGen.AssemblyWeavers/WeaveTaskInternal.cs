using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Antelcat.AutoGen.AssemblyWeavers.Weavers;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeavers;

internal static class WeaveTaskInternal
{
    public static bool Execute(IWaveArguments arguments, Action<string>? errorLog = null)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(arguments.AssemblyFile)!);
        using var assembly = AssemblyDefinition.ReadAssembly(arguments.AssemblyFile, new ReaderParameters
        {
            InMemory = true,
            SymbolReaderProvider = arguments.ReadWritePdb ? new PortablePdbReaderProvider() : default!,
            AssemblyResolver = resolver,
        });
        List<Exception> exceptions = [];
        foreach (var weaver in Weavers())
        {
            try
            {
                weaver.Execute(assembly);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            foreach (var exception in exceptions)
            {
#if DEBUG
                Debugger.Break();
#endif
                errorLog?.Invoke(exception.ToString());
            }

            return false;
        }

        var temp = Path.GetTempFileName();
        try
        {
            assembly.Write(temp, new WriterParameters
            {
                SymbolWriterProvider = arguments.ReadWritePdb ? new PortablePdbWriterProvider() : default!,
                WriteSymbols         = arguments.ReadWritePdb,
            });
            File.Copy(temp, arguments.AssemblyFile, true);
            return true;
        }
        catch (Exception exception)
        {
#if DEBUG
            throw;
#endif
            errorLog?.Invoke(exception.ToString());
            return false;
        }
    }

    private static IEnumerable<IWeaver> Weavers()
    {
        yield return new RecordPlaceboWeaver();
    }
}