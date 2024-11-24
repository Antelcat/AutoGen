using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Antelcat.AutoGen.AssemblyWeavers;

[Serializable]
public class WeaveTask : Task, IWaveArguments
{
    private static string Category = typeof(WeaveTask).FullName!;

    [Required] public required string  AssemblyFile              { get; set; }
    public                     string? AssemblyOriginatorKeyFile { get; set; }
    public                     bool    SignAssembly              { get; set; }
    public                     bool    DelaySign                 { get; set; }
    public                     bool    ReadWritePdb              { get; set; } = true;

    [Output] public string? Output { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, $"[{Category}] Weaving start");
        if (!WeaveTaskInternal.Execute(this, x => Log.LogError($"[{Category}] {x}"))) return false;
        Log.LogMessage(MessageImportance.High, $"[{Category}] Weaving complete");
        return true;
    }
}