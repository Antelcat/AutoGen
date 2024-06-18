using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapProvider : MapSide
{
    public MapProvider(IMethodSymbol method, ITypeSymbol type) : base(method, type)
    {
        ArgName = !method.ReturnsVoid
            && SymbolEqualityComparer.Default.Equals(type, method.ContainingType)
                ? "this" : method.Parameters[0].Name;
        AvailableProperties = type.GetAllProperties()
            .Where(x =>
                !x.IsStatic && !x.IsWriteOnly &&
                x.DeclaredAccessibility.IsIncludedIn(ActualAccess)).ToList();
    }

    public override string                       ArgName             { get; }
    public override IEnumerable<IPropertySymbol> AvailableProperties { get; }
}