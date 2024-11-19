using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeaver;

public abstract class Weaver
{
    public abstract void Execute(AssemblyDefinition assembly);

    public override string ToString() => GetType().Name;
}