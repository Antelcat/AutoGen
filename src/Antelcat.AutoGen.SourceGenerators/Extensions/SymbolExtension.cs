using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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
    
    public static List<INamedTypeSymbol> GetAllTypes(this IAssemblySymbol symbol)
    {
        var collector = new TypeCollector();
        symbol.GlobalNamespace.Accept(collector);
        return collector.Symbols;
    }

    private class TypeCollector : SymbolVisitor
    {
        public List<INamedTypeSymbol> Symbols { get; } = [];

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            Symbols.Add(symbol);
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }
    }
}