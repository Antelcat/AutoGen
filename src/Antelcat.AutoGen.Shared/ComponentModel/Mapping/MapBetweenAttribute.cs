using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Specify property mapping between two types
/// </summary>
/// <param name="fromProperty">property name in source type</param>
/// <param name="toProperty">property name in target type</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MapBetweenAttribute(string fromProperty, string toProperty) : Attribute
{
    internal readonly string FromProperty = fromProperty;
    internal readonly string ToProperty   = toProperty;
    
    /// <summary>
    /// Map through a converter
    /// </summary>
    public string? By { get; set ; }
}