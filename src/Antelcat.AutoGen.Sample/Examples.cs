using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample;

public static partial class Mapper
{
    [AutoMap(Extra = [nameof(Map)])]
    [MapConstructor(nameof(Dto.Name),nameof(Dto.Id))]
    [MapBetween(nameof(Dto.Name),nameof(Entity.KK))]
    [MapInclude(nameof(Entity.Id),typeof(Entity))]
    public static partial Entity Fun(this Dto d);

    private static void Map(Dto e, Entity d)
    {
        d.KK = d.Id.ToString();
    }
}
public partial class Entity
{
    public Entity(string name, int id) { }

    public required string KK { get; set; }

    [MapIgnore]
    public int Id { get; init; }

    internal int Number { get; set; }

    [AutoMap(Extra = [nameof(Ext)])]
    [MapBetween(nameof(KK), nameof(Dto.Name))]
    [MapInclude(nameof(Id),typeof(Entity))]
    private partial Dto ToDto();

    private void Ext(Dto d)
    {
        
    }
}

public partial class Dto
{
    internal string Name { get; set; }

    public int Id { get; set; }

    internal int Number { get; set; }

    private void Set(Entity e)
    {
    }
}
