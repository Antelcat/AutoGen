using System;
using System.Linq;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class TypeExtension
{
    public static string QualifiedFullName(this Type type) =>
        type.IsGenericParameter ? type.Name : $"{(type.Namespace is GlobalNamespace ? "" : $"{type.Namespace}.")}{type.Name.Split('`').First()}";

    public static string GlobalQualifiedFullName(this Type type) =>
        type.IsGenericParameter ? type.Name : global + type.QualifiedFullName();

}