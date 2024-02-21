using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;
// ReSharper disable IdentifierTypo

namespace Antelcat.AutoGen.ComponentModel;

[AttributeUsage(AttributeTargets.Assembly)]
public class AutoDeconstructIndexableAttribute(int size = 16, params Type[] indexableTypes) : AutoGenAttribute
{
    internal int    Size           => size;
    internal Type[] IndexableTypes => indexableTypes;

    public string Namespace { get; set; } = nameof(System);
}