using Antelcat.AutoGen.ComponentModel.Entity;

namespace Antelcat.AutoGen.Sample;

[GenerateMapBetween(typeof(Entity), typeof(Dto))]
public static class Mapper
{
    
}

[GenerateMapTo(typeof(Dto),Extra = [nameof(Map)])]
public partial class Entity
{
    [MapToName(nameof(Dto.Name), ValidOn = typeof(Entity))]
    public string KK { get; set; }

    [MapToName(nameof(Dto.id))]
    protected internal int Id { get; set; }

    private int Number { get; set; }

    private void Map(Dto o)
    {

    }
}

public class Dto
{
    public string Name { get; set; }
    
    public int id { get; set; }
    
    internal int Number { get; set; }
}