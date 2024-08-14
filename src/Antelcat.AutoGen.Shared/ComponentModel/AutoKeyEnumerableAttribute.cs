using System;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

/// <summary>
/// Auto generate key enumerable for the target type, default to properties
/// </summary>
/// <param name="name">generate property name</param>
/// <param name="memberTypes">types of exposed members</param>
/// <param name="includeInherited">whether include inherited members</param>
/// <param name="accessibility"></param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoKeyEnumerableAttribute(
    string name = "Keys",
    MemberTypes memberTypes = MemberTypes.Property,
    bool includeInherited = true,
    Accessibility accessibility = Accessibility.Public
) : AutoGenAttribute
{
    internal string        Name             => name;
    internal Accessibility Accessibility    => accessibility;
    internal bool          IncludeInherited => includeInherited;
    internal MemberTypes   MemberTypes      => memberTypes;

    /// <summary>
    /// Ignored members
    /// </summary>
    public string[]? Ignores { get; set; }
}