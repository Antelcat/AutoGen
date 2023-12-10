using System.Runtime.InteropServices;
using Antelcat.AutoGen.ComponentModel.Entity;

namespace Antelcat.AutoGen.Sample;

[GenerateMapBetween(typeof(Entity), typeof(Dto))]
public static class Mapper
{
    
}

[GenerateMapTo(typeof(Dto), Extra = [nameof(Map),nameof(Map2)])]
public partial class Entity
{
    [MapToName(nameof(Dto.Name), ValidOn = typeof(Entity))]
    public required string KK { get; set; }

    [MapToName(nameof(Dto.id))] 
    protected internal int Id { get; set; }

    private int Number { get; set; }

    private void Map(Dto o)
    {
    }

    private void Map2(Dto o)
    {
        
    }
}

[GenerateMapTo(typeof(Entity), Extra = [nameof(Set)])]
public partial class Dto
{
    [MapToName(nameof(Entity.KK))] internal string Name { get; set; }

    [MapToName(nameof(Entity.Id))] public int id { get; set; }

    internal int Number { get; set; }

    private void Set(Entity e)
    {
        e.Id = id;
    }
}