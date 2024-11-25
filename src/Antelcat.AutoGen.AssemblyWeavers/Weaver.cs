using System.Collections.Generic;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeavers;

public abstract class Weaver
{
    public required AssemblyDefinition AssemblyDefinition { get; init; }
    public abstract bool FilterMainModuleType(TypeDefinition typeDefinition);
    public abstract void Execute(IReadOnlyList<TypeDefinition> types);
}