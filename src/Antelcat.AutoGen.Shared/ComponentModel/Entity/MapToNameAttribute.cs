using System;

namespace Antelcat.AutoGen.ComponentModel.Entity;

/// <summary>
/// Specified property name of this map
/// </summary>
/// <param name="propertyName">map to property name</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class MapToNameAttribute(string propertyName) : Attribute
{
    internal string PropertyName = propertyName;
    
    /// <summary>
    /// Limit type valid on this mapper
    /// </summary>
    public Type? ValidOn { get; set; } 
}