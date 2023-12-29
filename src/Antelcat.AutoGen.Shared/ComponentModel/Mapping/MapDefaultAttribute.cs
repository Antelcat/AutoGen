namespace Antelcat.AutoGen.ComponentModel.Mapping;

public class MapDefaultAttribute(string property, object? value) : MapConfigAttribute(property)
{
    internal readonly object? Value = value;
}