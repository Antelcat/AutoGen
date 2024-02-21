using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class AutoWatchAttribute : AutoGenAttribute;