using System.Diagnostics.CodeAnalysis;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostics;

#if NET
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AutoRuntimeInfo(params Type[] types) : AutoGenAttribute
{
    internal Type[] Types => types;
    
    public DynamicallyAccessedMemberTypes MemberTypes { get; set; } = DynamicallyAccessedMemberTypes.All;
}
#endif
