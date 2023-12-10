using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using Antelcat.AutoGen.ComponentModel.Entity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.AutoGen.Sample;

[GenerateMapBetween(typeof(Entity), typeof(Dto))]
public static class Mapper
{
}

[GenerateMapTo(typeof(Dto), Extra = [nameof(Map), nameof(Map2)])]
public partial class Entity
{
    [MapToName(nameof(Dto.Name), ValidOn = typeof(Dto))]
    public required string KK { get; set; }

    [MapToName(nameof(Dto.id))] protected internal int Id { get; set; }

    [MapIgnore] private int Number { get; set; }

    private void Map(Dto o)
    {
    }

    private void Map2(Dto o)
    {
    }
}

[GenerateMapTo(typeof(Entity), Extra = [nameof(Set)])]
[GenerateMapTo(typeof(Dto), Alias = nameof(Copy))]
public partial class Dto
{
    [MapToName(nameof(Entity.KK))] internal string Name { get; set; }

    [MapIgnore(typeof(Dto))] public int id { get; set; }

    internal int Number { get; set; }

    private void Set(Entity e)
    {
        e.Id = id;
    }
}