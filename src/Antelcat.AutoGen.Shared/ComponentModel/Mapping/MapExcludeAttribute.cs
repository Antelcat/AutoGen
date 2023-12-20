using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Specify property ignored in mapping
/// </summary>
/// <param name="property">Name of the property</param>
public class MapExcludeAttribute(string property) : MapConfigAttribute(property);
