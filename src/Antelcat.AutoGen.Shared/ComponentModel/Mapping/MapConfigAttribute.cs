using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class MapConfigAttribute(string property, Type belongsTo) : Attribute
{
    internal readonly string Property  = property;
    internal readonly Type   BelongsTo = belongsTo;
}