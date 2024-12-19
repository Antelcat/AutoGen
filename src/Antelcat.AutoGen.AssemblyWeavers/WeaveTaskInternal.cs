using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            SymbolReaderProvider = arguments.ReadWritePdb ? new PortablePdbReaderProvider() : null!,
            AssemblyResolver = resolver,
        });
        List<Exception> exceptions    = [];

        var weaverContext = Weavers(assembly)
            .Select(weaver => (weaver, new List<TypeDefinition>()))
            .ToArray();
        
        foreach (var mainModuleType in assembly.MainModule.Types)
        {
            foreach (var (weaver, typeDefinitions) in weaverContext)
            {
                if (weaver.FilterMainModuleType(mainModuleType)) typeDefinitions.Add(mainModuleType);
            }
        }
        foreach (var(weaver, typeDefinitions) in weaverContext)
        {
            try
            {
                weaver.Execute(typeDefinitions);
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
                SymbolWriterProvider = arguments.ReadWritePdb ? new PortablePdbWriterProvider() : null!,
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

    private static IEnumerable<Weaver> Weavers(AssemblyDefinition assembly)
    {
        yield return new RecordPlaceboWeaver
        {
            AssemblyDefinition = assembly
        };
    }
    
    
}