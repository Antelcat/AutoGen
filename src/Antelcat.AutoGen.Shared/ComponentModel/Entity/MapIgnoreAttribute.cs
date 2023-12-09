using System;

namespace Antelcat.AutoGen.ComponentModel.Entity;

[AttributeUsage(AttributeTargets.Property,AllowMultiple = true)]
public class MapIgnoreAttribute(params Type[] when) : Attribute;