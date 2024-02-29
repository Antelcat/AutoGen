using System.Collections.Generic;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapConfigInfo
{
    public MapConfigInfo(IEnumerable<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass!.ToDisplayString() == MapperGenerator.MapExclude)
            {
                var attr = attribute.ToAttribute<MapExcludeAttribute>();
                var name = attr.Property;
                Excludes.Add(name);
            }

            if (attribute.AttributeClass!.ToDisplayString() == MapperGenerator.MapInclude)
            {
                var attr = attribute.ToAttribute<MapIncludeAttribute>();
                var name = attr.Property;
                Includes.Add(name);
            }
        }
    }

    public HashSet<string> Excludes { get; } = new();
    public HashSet<string> Includes { get; } = new();
}