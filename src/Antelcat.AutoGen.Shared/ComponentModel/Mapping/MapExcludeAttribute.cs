using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Specify property ignored in mapping
/// </summary>
/// <param name="belongsTo"><see cref="Type"/> which the property belongs to</param>
/// <param name="property">Name of the property</param>
public class MapExcludeAttribute(string property, Type belongsTo) : MapConfigAttribute(property, belongsTo);
