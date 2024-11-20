using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeaver;

public interface IWeaver
{
    void Execute(AssemblyDefinition assembly);
}