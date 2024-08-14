using System;

namespace Antelcat.AutoGen.ComponentModel.Mapping;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.ReturnValue | AttributeTargets.Parameter,
                AllowMultiple = true)]
public abstract class MapConfigAttribute(string property) : Attribute
{
    internal string Property => property;
}