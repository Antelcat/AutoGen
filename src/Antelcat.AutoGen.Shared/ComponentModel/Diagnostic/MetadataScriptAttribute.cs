using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MetadataScriptAttribute(params object[] args) : AutoGenAttribute
{
    internal readonly object[] Args = args;

    public string? FileName { get; set; }
}