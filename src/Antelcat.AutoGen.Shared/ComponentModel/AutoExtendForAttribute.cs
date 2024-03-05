using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class AutoExtendForAttribute(Type type,params string[]? methods) : AutoGenAttribute
{
    internal Type Type => type;

    internal string[]? Methods => methods;

    /// <summary>
    /// The namespace of the target type when marked on assembly
    /// </summary>
    public string Namespace { get; set; } = nameof(System);

    public Type[]? ParameterTypes { get; set; }
}