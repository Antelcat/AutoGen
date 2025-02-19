using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Antelcat.AutoGen.AssemblyWeavers.Weavers;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeavers;

internal static class WeaveTaskInternal
{
    public static List<string> SplitUpReferences(string references, TaskLogger logger)
    {
        var splitReferences = references
            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        logger.LogDebug("Reference count: " + splitReferences.Count);

        var joinedReferences = string.Join(Environment.NewLine + "  ", splitReferences.OrderBy(_ => _));
        logger.LogDebug($"References:{Environment.NewLine}  {joinedReferences}");
        return splitReferences;
    }

    public static bool Execute(IWaveArguments arguments, TaskLogger logger, CancellationToken token)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(arguments.AssemblyFile)!);
        token.ThrowIfCancellationRequested();
        using var module = ModuleDefinition.ReadModule(arguments.AssemblyFile, new ReaderParameters
        {
            InMemory             = true,
            SymbolReaderProvider = arguments.ReadWritePdb ? new PortablePdbReaderProvider() : null!,
            AssemblyResolver     = new AssemblyResolver(logger, SplitUpReferences(arguments.References, logger)),
        });
        try
        {
            module.ReadSymbols();
        }
        catch
        {
            //
        }
        token.ThrowIfCancellationRequested();

        List<Exception> exceptions = [];

        var weaverContext = Weavers(module)
            .Select(weaver => (weaver, new List<TypeDefinition>()))
            .ToArray();
        token.ThrowIfCancellationRequested();

        foreach (var mainModuleType in module.Types)
        {
            foreach (var (weaver, typeDefinitions) in weaverContext)
            {
                token.ThrowIfCancellationRequested();
                if (weaver.FilterMainModuleType(mainModuleType)) typeDefinitions.Add(mainModuleType);
            }
        }
        foreach (var(weaver, typeDefinitions) in weaverContext)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                weaver.Execute(typeDefinitions);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            token.ThrowIfCancellationRequested();
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
        token.ThrowIfCancellationRequested();
        try
        {
            var strongKeyFinder = arguments.SignAssembly ? new StrongKeyFinder(arguments, module,logger) : null;
            strongKeyFinder?.FindStrongNameKey();
            if (strongKeyFinder?.PublicKey is not null)
            {
                module.Assembly.Name.PublicKey = strongKeyFinder.PublicKey;
            }
            module.Write(temp, new WriterParameters
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

    private static IEnumerable<Weaver> Weavers(ModuleDefinition module)
    {
        yield return new RecordPlaceboWeaver
        {
            AssemblyDefinition = module.Assembly
        };
    }
    
    
}