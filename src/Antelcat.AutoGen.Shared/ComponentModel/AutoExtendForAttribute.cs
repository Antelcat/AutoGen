using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class AutoExtendForAttribute(Type type, string? methodName = null) : AutoGenAttribute
{
    internal Type Type => type;

    internal string? MethodName => methodName;

    public string? Namespace { get; set; } = nameof(System);

    /// <summary>
    /// Array for overloads
    /// </summary>
    public Type[][]? ArgumentTypes { get; set; }
}