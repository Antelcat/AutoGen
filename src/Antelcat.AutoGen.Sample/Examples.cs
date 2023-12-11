using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample;

public static partial class Mapper
{
    [GenerateMap]
    [MapBetween(nameof(Entity.KK), nameof(Dto.Name))]
    [MapInclude(nameof(Entity.Id),typeof(Entity))]
    public static partial Entity Fun(this Dto d);
}

public partial class Entity
{
    public required string KK { get; set; }

    [MapIgnore]
    public int Id { get; set; }

    internal int Number { get; set; }

    [GenerateMap]
    [MapBetween(nameof(KK), nameof(Dto.Name))]
    private partial Dto ToDto();
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
