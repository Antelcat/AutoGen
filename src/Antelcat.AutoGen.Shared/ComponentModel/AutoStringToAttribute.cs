using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel;

/// <summary>
/// Auto generate string.To() extension method, which should be <see langword="partial"/> <see langword="static"/>
/// </summary>
/// <param name="namespace"></param>
/// <param name="accessibility">Specify the accessibility of the extension, accepts only <see cref="Accessibility.Public"/> or <see cref="Accessibility.Internal"/> </param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public sealed class AutoStringToAttribute(
    string? @namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal readonly string Namespace = @namespace ?? nameof(System);

    internal readonly Accessibility Accessibility = accessibility;
}