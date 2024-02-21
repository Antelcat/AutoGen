using System;
using System.Linq;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class TypeExtension
{
    public static string QualifiedFullName(this Type type) => $"{type.Namespace}.{type.Name.Split('`').First()}";
    
}