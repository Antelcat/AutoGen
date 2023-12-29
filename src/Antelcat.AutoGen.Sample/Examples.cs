using System;
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

    public             string? Name   { get; set; }
    public required    string  KK     { get; set; }
    [MapIgnore] public int     Id     { get; init; }
    internal           int     Number { get; set; }

    [AutoMap]
    [MapBetween(nameof(KK), nameof(Dto.Name))]
    [MapInclude(nameof(Id))]
    private partial Dto ToDto();

    public Dto Test() =>
        new()
        {
            Id     = 1,
            Name   = "123",
            Number = 123,
        };
}

public partial class Dto
{
    internal string Name   { get; set; }
    public   int    Id     { get; set; }
    internal int    Number { get; set; }
}


[Flags]
public enum E
{
    Normal       = 0x0,
    Access       = 0x1,
    Removed      = 0x2,
    AccessDenied = 0x4
}