namespace Antelcat.AutoGen.AssemblyWeavers;

public abstract class TaskLogger
{
    public abstract void LogDebug(string message);

    public abstract void LogWarning(string message);

    public abstract void LogError(string message);
}