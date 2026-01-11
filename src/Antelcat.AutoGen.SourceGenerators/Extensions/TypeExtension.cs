using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class TypeExtension
{
    extension(Type type)
    {
        public string QualifiedFullName() =>
            type.IsGenericParameter ? type.Name : $"{(type.Namespace is null ? "" : $"{type.Namespace}.")}{type.Name.Split('`').First()}";
        public string GlobalQualifiedFullName() =>
            type.IsGenericParameter ? type.Name : global + type.QualifiedFullName();
        public string QualifiedFullFileName() => type
            .QualifiedFullName()
            .Replace('<', '{')
            .Replace('>', '}');
        public string QualifiedSymbolFullName() => type is Feast.CodeAnalysis.CompileTime.Type t
            ? t.Symbol.GetFullyQualifiedName()
            : type.QualifiedFullName();
    }

}