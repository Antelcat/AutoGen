using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

/// <summary>
/// Auto generate object clone extension methods, which should be <see langword="partial"/> <see langword="static"/> <see langword="unsafe"/>
/// </summary>
/// <param name="namespace"></param>
/// <param name="accessibility"></param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public class AutoObjectCloneAttribute(
    string? @namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal readonly string Namespace = @namespace ?? nameof(System);

    internal readonly Accessibility Accessibility = accessibility;
}