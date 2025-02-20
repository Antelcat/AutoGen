using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Antelcat.AutoGen.AssemblyWeavers.Exceptions;
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
        module.ReadSymbols();
        token.ThrowIfCancellationRequested();


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

        List<WeaverException> stashedExceptions = []; //stashed exceptions

        foreach (var (weaver, typeDefinitions) in weaverContext)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                weaver.Execute(typeDefinitions);
            }
            catch (WeaverException ex)
            {
                stashedExceptions.Add(ex);
            }
            catch (Exception ex)
            {
                stashedExceptions.Add(new WeaverException(ex)
                {
                    WeaverName = weaver.Name
                });
            }
        }
        var temp = Path.GetTempFileName();
        token.ThrowIfCancellationRequested();

        var strongKeyFinder = arguments.SignAssembly ? new StrongKeyFinder(arguments, module, logger) : null;
        strongKeyFinder?.FindStrongNameKey();
        if (strongKeyFinder?.PublicKey is not null)
        {
            module.Assembly.Name.PublicKey = strongKeyFinder.PublicKey;
        }

        module.Write(temp, new WriterParameters
        {
            SymbolWriterProvider = arguments.ReadWritePdb ? new PortablePdbWriterProvider() : null!,
            WriteSymbols         = arguments.ReadWritePdb,
            StrongNameKeyPair    = strongKeyFinder?.StrongNameKeyPair!
        });
        File.Copy(temp, arguments.AssemblyFile, true);

        if (stashedExceptions.Count > 0) throw new WeaverExceptions(stashedExceptions);
        return true;
    }

    private static IEnumerable<Weaver> Weavers(ModuleDefinition module)
    {
        yield return new RecordPlaceboWeaver
        {
            AssemblyDefinition = module.Assembly
        };
    }
    
    
}