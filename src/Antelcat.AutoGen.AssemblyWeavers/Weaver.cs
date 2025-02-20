using System.Collections.Generic;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeavers;

public abstract class Weaver
{
    public abstract string Name { get; }
    public required AssemblyDefinition AssemblyDefinition { get; init; }
    public abstract bool FilterMainModuleType(TypeDefinition typeDefinition);
    public abstract void Execute(IReadOnlyList<TypeDefinition> types);
}