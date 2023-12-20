using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample;

public static partial class Mapper
{
    [AutoMap(Extra = [nameof(Map)])]
    [MapConstructor(nameof(Dto.Name), nameof(Dto.Id))]
    [MapBetween(nameof(Dto.Name), nameof(Entity.KK))]
    [return: MapExclude(nameof(Entity.Name))]
    public static partial Entity ToEntity([MapInclude(nameof(Dto.Id))] Dto d);
    
    private static void Map(Dto e, Entity d)
    {
        d.KK = d.Id.ToString();
    }
}
public partial class Entity
{
    public Entity(string name, int id) { }

    public string? Name { get; set; }
    public required string KK { get; set; }
    

    [MapIgnore]
    public int Id { get; init; }

    internal int Number { get; set; }

    [AutoMap(Extra = [nameof(Ext)])]
    [MapBetween(nameof(KK), nameof(Dto.Name))]
    [MapInclude(nameof(Id))]
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
