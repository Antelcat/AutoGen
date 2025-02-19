using System;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Antelcat.AutoGen.AssemblyWeavers;

[Serializable]
public class WeaveTask : Task, ICancelableTask, IWaveArguments
{
    private static string category = typeof(WeaveTask).FullName!;

    private readonly CancellationTokenSource cancel = new();
    
    [Required] public required string  AssemblyFile              { get; set; }
    public                     string? AssemblyOriginatorKeyFile { get; set; }
    public                     bool    SignAssembly              { get; set; }
    public                     bool    DelaySign                 { get; set; }
    public                     bool    ReadWritePdb              { get; set; } = true;
    public                     string? IntermediateDirectory     { get; set; }
    [Required] public required string  References                { get; set; }

    [Output] public string? Output { get; set; }

    public override bool Execute()
    {
        var name = Path.GetFileName(AssemblyFile);
        Log.LogMessage(MessageImportance.High, $"[{category}] Weaving start : {name}");
        try
        {
            if (!WeaveTaskInternal.Execute(this, new Logger(Log),cancel.Token)) return false;
            Log.LogMessage(MessageImportance.High, $"[{category}] Weaving complete : {name}");
        }
        catch
        {
            //
            return false;
        }

        return true;
    }

    public void Cancel() => cancel.Cancel();

    private class Logger(TaskLoggingHelper logger) : AssemblyWeavers.TaskLogger
    {
        public override void LogDebug(string message)
        {
            logger.LogMessage(MessageImportance.Low, $"[{category}] {message}");
        }

        public override void LogWarning(string message)
        {
            logger.LogWarning($"[{category}] {message}");
        }

        public override void LogError(string message)
        {
            logger.LogError($"[{category}] {message}");
        }
    }
}