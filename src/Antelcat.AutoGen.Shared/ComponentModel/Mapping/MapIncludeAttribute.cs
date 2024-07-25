namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Make property marked <see cref="MapIncludeAttribute"/> included in mapping
/// When map self , this should be marked on method
/// </summary>
/// <param name="property">Property name to be included</param>
public class MapIncludeAttribute(string property) : MapConfigAttribute(property);