using System;
namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Make property marked <see cref="MapIgnoreAttribute"/> included in mapping
/// </summary>
/// <param name="property"><see cref="Type"/> which the property belongs to</param>
/// <param name="belongsTo">Name of the property</param>
public class MapIncludeAttribute(string property, Type belongsTo) : Attribute;