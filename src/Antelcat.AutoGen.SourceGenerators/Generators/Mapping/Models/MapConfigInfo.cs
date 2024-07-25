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
                Excludes.Add(attribute.ToAttribute<MapExcludeAttribute>().Property);

            if (attribute.AttributeClass!.ToDisplayString() == MapperGenerator.MapInclude)
                Includes.Add(attribute.ToAttribute<MapIncludeAttribute>().Property);
        }
    }

    public HashSet<string> Excludes { get; } = [];
    public HashSet<string> Includes { get; } = [];
}