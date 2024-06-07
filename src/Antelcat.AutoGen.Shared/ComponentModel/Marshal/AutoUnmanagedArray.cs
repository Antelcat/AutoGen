using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Marshal;

[AttributeUsage(AttributeTargets.Struct)]
public class AutoUnmanagedArray : AutoGenAttribute
{
    public AutoUnmanagedArray(
        Type unmanagedType,
        int size,
        Accessibility accessibility = Accessibility.Public) : this(size, accessibility)
    {
        UnmanagedType = unmanagedType;
    }

    public AutoUnmanagedArray(
        string templateType,
        int size,
        Accessibility accessibility = Accessibility.Public) : this(size, accessibility)
    {
        TemplateType = templateType;
    }

    private AutoUnmanagedArray(int size,
        Accessibility accessibility)
    {
        Size          = size;
        Accessibility = accessibility;
    }

    internal readonly Type?         UnmanagedType;
    internal readonly string?       TemplateType;
    internal readonly int           Size;
    internal readonly Accessibility Accessibility;
}
