using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class SymbolExtension
{
    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
    {
        var members = type.GetMembers();
        return type.BaseType == null
            ? members
            : members
                .Concat(type.BaseType.GetAllMembers());
    }

    public static IEnumerable<IPropertySymbol> GetAllProperties(this ITypeSymbol type) =>
        type.GetAllMembers()
            .OfType<IPropertySymbol>();

    public static string Call(this IMethodSymbol method, params string[] args) =>
        (method.IsStatic
            ? $"{method.ContainingType.GetFullyQualifiedName()}.{method.Name}"
            : $"this.{method.Name}"
        )
        + $"({string.Join(", ", args)})";
}