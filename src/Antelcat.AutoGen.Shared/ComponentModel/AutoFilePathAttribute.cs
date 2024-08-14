using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Assembly)]
public class AutoFilePathAttribute(
    string? @namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal string Namespace => @namespace ?? nameof(System);

    internal Accessibility Accessibility => accessibility;
}
