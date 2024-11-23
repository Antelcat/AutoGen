using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeavers;

public interface IWeaver
{
    void Execute(AssemblyDefinition assembly);
}