using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Auto generate To() method from current to target
/// </summary>
/// <param name="fromAccess">the property with which accessibility included in the map</param>
[AttributeUsage(AttributeTargets.Method)]
public class AutoMapAttribute(
    Accessibility fromAccess =
        Accessibility.Public | Accessibility.Internal | Accessibility.Protected | Accessibility.Private)
    : AutoGenAttribute
{
    internal readonly Accessibility FromAccess = fromAccess;

    /// <summary>
    /// Specified the most strict target property's accessibility, only accept <see cref="Accessibility.Public"/> or <see cref="Accessibility.Internal"/>
    /// </summary>
    public Accessibility ToAccess { get; set; } = Accessibility.Internal | Accessibility.Public;

    /// <summary>
    /// Extra mapper methods' name, should be one argument of target type
    /// </summary>
    public string[]? Extra { get; set; }
}