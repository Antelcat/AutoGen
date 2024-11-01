using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class TypeExtension
{
    public static string QualifiedFullName(this Type type) =>
        type.IsGenericParameter ? type.Name : $"{(type.Namespace is null ? "" : $"{type.Namespace}.")}{type.Name.Split('`').First()}";

    public static string GlobalQualifiedFullName(this Type type) =>
        type.IsGenericParameter ? type.Name : global + type.QualifiedFullName();

    public static string QualifiedFullFileName(this Type type) => type
        .QualifiedFullName()
        .Replace('<', '{')
        .Replace('>', '}');

    public static string QualifiedSymbolFullName(this Type type) => type is Feast.CodeAnalysis.CompileTime.Type t
        ? t.Symbol.GetFullyQualifiedName()
        : type.QualifiedFullName();
}