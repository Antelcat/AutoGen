using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

/// <summary>
/// Auto generate default like
/// <code>
/// public override string ToString() => GetType().ToString();
/// </code>
/// and
/// <code>
/// public override int GetHashCode() => base.GetHashCode();
/// </code>
/// for all records marked with 'partial' when not explicitly declare these members
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
public class AutoRecordPlaceboAttribute : AutoGenAttribute;