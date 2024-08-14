using System;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

/// <summary>
/// Auto generate key accessor for the target type, default to properties
/// </summary>
/// <param name="includeInherited">include inherited members</param>
/// <param name="memberTypes">include member types, default is <see cref="MemberTypes.Property"/></param>
/// <param name="accessibility">this property accessibility</param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoKeyAccessorAttribute(
    MemberTypes memberTypes = MemberTypes.Property,
    bool includeInherited = true,
    Accessibility accessibility = Accessibility.Public)
    : AutoGenAttribute
{
    internal Accessibility Accessibility    => accessibility;
    internal bool          IncludeInherited => includeInherited;
    internal MemberTypes   MemberTypes      => memberTypes;

    /// <summary>
    /// accessor can get, default true
    /// </summary>
    public bool Get { get; set; } = true;

    /// <summary>
    /// accessor can set, default true
    /// </summary>
    public bool Set { get; set; } = true;

    /// <summary>
    /// Ignored members
    /// </summary>
    public string[]? Ignores { get; set; }
}