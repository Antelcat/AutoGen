using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

/// <summary>
/// Auto generate key accessor for the target type, default to properties
/// </summary>
/// <param name="includeInherited">include inherited members</param>
/// <param name="includeField">include fields</param>
/// <param name="accessibility">this property accessibility</param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoKeyAccessor(
    bool includeInherited = true,
    bool includeField = false,
    Accessibility accessibility = Accessibility.Public)
    : AutoGenAttribute
{
    internal readonly Accessibility Accessibility    = accessibility;
    internal readonly bool          IncludeInherited = includeInherited;
    internal readonly bool          IncludeField     = includeField;

    /// <summary>
    /// accessor can get, default true
    /// </summary>
    public bool Get { get; set; } = true;
    
    /// <summary>
    /// accessor can set, default true
    /// </summary>
    public bool Set { get; set; } = true;
    
    public string[]? Ignores { get; set; }
    
}