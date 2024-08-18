using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Antelcat.AutoGen.Native;

public class RuntimeExtension
{
    public static MemberInfo[] GetMembers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        BindingFlags flags)
    {
        return typeof(T).GetMembers(flags);
    }
}