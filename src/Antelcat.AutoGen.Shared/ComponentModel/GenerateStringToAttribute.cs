using System;

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class GenerateStringToAttribute(string? Namespace = nameof(System),
    Accessibility accessibility = Accessibility.Public) : Attribute;
