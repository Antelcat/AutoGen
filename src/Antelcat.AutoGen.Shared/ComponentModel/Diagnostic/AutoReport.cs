using System;
using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

/// <summary>
/// Auto generate a report for the method
/// </summary>
/// <param name="include">Manual include members</param>
[AttributeUsage(AttributeTargets.Method)]
internal class AutoReport(params string[] include) : AutoMapAttribute
{
    public string[] Ignore { get; set; } = [];

    public delegate void ReportHandler(string memberName, Type memberType, MemberKind memberKind,
        Func<object?>? valueGetter);

    public enum MemberKind
    {
        Field,
        Property,
        Method,
        NestedType,
    }
}
