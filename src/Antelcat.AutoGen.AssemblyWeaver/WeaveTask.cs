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
    public static void Execute(string target, Action<string>? errorLog = null)
    {
        var resolver =  new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(target)!);
        using var assembly = AssemblyDefinition.ReadAssembly(target, new ReaderParameters
        {
            InMemory = true,
            SymbolReaderProvider = new PdbReaderProvider(),
            AssemblyResolver = resolver
        });
        foreach (var weaver in Weavers())
        {
            try
            {
                weaver.Execute(assembly);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                errorLog?.Invoke($"{weaver} : {ex}");
            }
        }
        assembly.Write(target, new WriterParameters
        {
            SymbolWriterProvider = new PortablePdbWriterProvider(),
            WriteSymbols = true
        });
    }
    
    private static IEnumerable<IWeaver> Weavers()
    {
        yield return new RecordPlaceboWeaver();
    }
}

[Serializable]
public class WeaveTask : Task
{
    private const string Category = $"{nameof(Antelcat)}.{nameof(AutoGen)}.{nameof(AssemblyWeaver)}.{nameof(WeaveTask)}";

    [Required] public string Target { get; set; }

    [Output] public string Output { get; set; }

    public override bool Execute()
    {
        WeaveTaskInternal.Execute(Target, x => Log.LogError($"[{Category}] {x}"));
        return true;
    }
}