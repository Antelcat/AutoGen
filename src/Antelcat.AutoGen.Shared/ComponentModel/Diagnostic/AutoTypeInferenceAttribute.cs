using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.Assembly)]
public class AutoTypeInferenceAttribute(
    string? @namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal readonly string Namespace = @namespace ?? nameof(System);

    internal readonly Accessibility Accessibility = accessibility;

    /// <summary>
    /// Type only be created with specified prefix
    /// </summary>
    public string? Prefix { get; set; }

    public TypeKind Kind { get; set; } = TypeKind.Class;
    
    public enum TypeKind
    {
        Class,
        Record
    }
}