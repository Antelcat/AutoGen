using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Auto generate To() method from current to target
/// </summary>
/// <param name="exportFrom">the property with which accessibility included in the map</param>
[AttributeUsage(AttributeTargets.Method)]
public class GenerateMapAttribute(
    Accessibility exportFrom =
        Accessibility.Public | Accessibility.Internal | Accessibility.Protected | Accessibility.Private)
    : GenerateAttribute
{
    internal readonly Accessibility ExportFrom = exportFrom;

    /// <summary>
    /// Specified the most strict target property's accessibility, only accept <see cref="Accessibility.Public"/> or <see cref="Accessibility.Internal"/>
    /// </summary>
    public Accessibility ExportTo { get; set; } = Accessibility.Internal | Accessibility.Public;

    /// <summary>
    /// Extra mapper methods' name, should be one argument of target type
    /// </summary>
    public string[]? Extra { get; set; }
}