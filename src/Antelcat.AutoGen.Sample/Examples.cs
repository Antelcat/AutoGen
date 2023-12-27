using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample;

public static partial class Mapper
{
    [AutoMap(Extra = [nameof(Map)])]
    [MapConstructor(nameof(Dto.Name), nameof(Dto.Id))]
    [MapBetween(nameof(Dto.Name), nameof(Entity.KK), By = nameof(Convert))]
    [return: MapExclude(nameof(Entity.Name))]
    public static partial Entity ToEntity([MapInclude(nameof(Dto.Id))] Dto d);

    private static void Map(Dto e, Entity d)
    {
        d.KK = d.Id.ToString();
    }

    private static string Convert(string source) => source + "123";
}

public partial class Entity
{
    public Entity(string name, int id)
    {
    }

    public          string? Name { get; set; }
    public required string  KK   { get; set; }


    [MapIgnore] public int Id { get; init; }

    internal int Number { get; set; }

    [AutoMap(Extra = [nameof(Ext)])]
    [MapBetween(nameof(KK), nameof(Dto.Name), By = nameof(Transform))]
    [MapInclude(nameof(Id))]
    private partial Dto ToDto();

    private void Ext(Dto d)
    {
    }

    private static string Transform(string str) => string.Concat("");
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