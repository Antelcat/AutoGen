using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RecordExcludeAttribute : AutoGenAttribute
{
}