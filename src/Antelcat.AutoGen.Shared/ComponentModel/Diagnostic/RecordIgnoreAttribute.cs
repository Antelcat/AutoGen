using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

/// <summary>
/// Members marked this attribute will be excluded in <see cref="object.GetHashCode"/>
/// and PrintMembers(StringBuilder)
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RecordIgnoreAttribute : AutoGenAttribute;