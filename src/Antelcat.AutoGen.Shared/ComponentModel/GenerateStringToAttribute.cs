using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class GenerateStringToAttribute(
    string? @namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : GenerateAttribute
{
    internal readonly string Namespace = @namespace ?? nameof(System);

    internal readonly Accessibility Accessibility = accessibility;
}