#nullable enable
using System;
using System.Collections.Generic;
using Antelcat.AutoGen.AssemblyWeaver.Weavers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeaver;

internal static class WeaveTaskInternal
{
    public static void Execute(string target, Action<string>? errorLog = null)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(target, new ReaderParameters
        {
            InMemory = true
        });
        foreach (var weaver in Weavers())
        {
            try
            {
                weaver.Execute(assembly);
            }
            catch (Exception ex)
            {
                errorLog?.Invoke($"{weaver} : {ex}");
            }
        }
        assembly.Write(target);
    }
    
    private static IEnumerable<Weaver> Weavers()
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
        //WeaveTaskInternal.Execute(Target, x => Log.LogError($"[{Category}] {x}"));
        return true;
    }

}