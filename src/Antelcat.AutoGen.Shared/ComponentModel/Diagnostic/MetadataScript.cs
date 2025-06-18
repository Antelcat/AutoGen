using System;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class MetadataScript(params object[] args) : AutoGenAttribute
{
    internal readonly object[] Args = args;

    protected MetadataScript() : this([]) { }

    public virtual object? Execute(params object[] Value) => string.Empty;
}