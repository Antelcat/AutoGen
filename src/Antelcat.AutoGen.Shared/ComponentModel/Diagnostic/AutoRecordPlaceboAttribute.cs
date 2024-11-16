using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

/// <summary>
/// Auto generate default <see cref="object.ToString"/> and <see cref="object.GetHashCode"/>
/// for all records marked with 'partial'
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class AutoRecordPlaceboAttribute : AutoGenAttribute;