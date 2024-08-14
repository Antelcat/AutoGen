using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = true)]
public class MapDefaultAttribute(string property, object? value = default) : MapConfigAttribute(property)
{
    internal object? Value => value;
}