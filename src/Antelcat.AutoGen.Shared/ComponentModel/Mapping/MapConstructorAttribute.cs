using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Explicitly specify the constructor used in mapping, and the order of the parameters
/// </summary>
/// <param name="propertyNames"></param>
[AttributeUsage(AttributeTargets.Method)]
public class MapConstructorAttribute(params string[] propertyNames) : Attribute
{
    internal string[] PropertyNames => propertyNames;

    public string?[]? Bys { get; set; }
}