using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

/// <summary>
/// Property ignored will no longer be included in any mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MapIgnoreAttribute : Attribute;