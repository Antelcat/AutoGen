using System.Linq;
using System.Resources;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeaver.Weavers;

public class RecordPlaceboWeaver : Weaver
{
    public override void Execute(AssemblyDefinition assembly)
    {
        var records = assembly.MainModule
            .Types
            .Where(x => x.IsRecord());
        foreach (var type in records)
        {
        }
    }

    private void Rewrite(TypeDefinition type)
    {
        
    }
}