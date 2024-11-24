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
    public static bool Execute(IWaveArguments arguments, TaskLogger logger)
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
                logger.LogError(exception.ToString());
            }

            return false;
        }

        var temp = Path.GetTempFileName();
        try
        {
            var strongKeyFinder = arguments.SignAssembly ? new StrongKeyFinder(arguments, assembly.MainModule,logger) : null;
            strongKeyFinder?.FindStrongNameKey();
            if (strongKeyFinder?.PublicKey is not null)
            {
                assembly.Name.PublicKey = strongKeyFinder.PublicKey;
            }
            assembly.Write(temp, new WriterParameters
            {
                SymbolWriterProvider = arguments.ReadWritePdb ? new PortablePdbWriterProvider() : default!,
                WriteSymbols         = arguments.ReadWritePdb,
                StrongNameKeyPair = strongKeyFinder?.StrongNameKeyPair!
            });
            File.Copy(temp, arguments.AssemblyFile, true);
            return true;
        }
        catch (Exception exception)
        {
#if DEBUG
            throw;
#endif
            logger.LogError(exception.ToString());
            return false;
        }
    }

    private static IEnumerable<IWeaver> Weavers()
    {
        yield return new RecordPlaceboWeaver();
    }
    
    
}