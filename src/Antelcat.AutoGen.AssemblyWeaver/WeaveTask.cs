using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Antelcat.AutoGen.AssemblyWeaver.Weavers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;

namespace Antelcat.AutoGen.AssemblyWeaver;

internal static class WeaveTaskInternal
{
    public static bool Execute(WeaveTask task, Action<string>? errorLog = null)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(task.AssemblyFile)!);
        using var assembly = AssemblyDefinition.ReadAssembly(task.AssemblyFile, new ReaderParameters
        {
            InMemory             = true,
            SymbolReaderProvider = new PdbReaderProvider(),
            AssemblyResolver     = resolver,
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
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                WriteSymbols         = true,
            });
            File.Copy(temp, task.AssemblyFile, true);
            return true;
        }
        catch (Exception exception)
        {
#if DEBUG
            Debugger.Break();
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

[Serializable]
public class WeaveTask : Task
{
    private const string Category =
        $"{nameof(Antelcat)}.{nameof(AutoGen)}.{nameof(AssemblyWeaver)}.{nameof(WeaveTask)}";

    [Required] public required string  AssemblyFile              { get; set; }
    public string? AssemblyOriginatorKeyFile { get; set; }

    [Output] public string? Output { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"[{Category}] Weaving start");
        if (!WeaveTaskInternal.Execute(this, x => Log.LogError($"[{Category}] {x}"))) return false;
        Log.LogMessage(MessageImportance.High, $"[{Category}] Weaving complete");
        return true;
    }
}