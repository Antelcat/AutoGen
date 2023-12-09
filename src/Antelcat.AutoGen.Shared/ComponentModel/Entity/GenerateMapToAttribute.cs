using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Entity;

/// <summary>
/// Auto generate To() method from current and target
/// </summary>
/// <param name="target">target type</param>
/// <param name="accessibility">the property with which accessibility included in the map</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class GenerateMapToAttribute(
    Type target,
    Accessibility accessibility =
        Accessibility.Public | Accessibility.Internal | Accessibility.Protected | Accessibility.Private)
    : GenerateAttribute
{
    internal readonly Accessibility Accessibility = accessibility;
    public            Accessibility TargetAccessibility { get; set; } = Accessibility.Internal;

    /// <summary>
    /// Alias naming
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Extra mapper methods' name, should be one argument of target type
    /// </summary>
    public string[]? Extra { get; set; }
}