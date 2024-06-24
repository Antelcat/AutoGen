using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Utils;

internal class PropertyNameComparer : IEqualityComparer<IPropertySymbol>
{
    public static PropertyNameComparer Default { get; } = new();
    public bool Equals(IPropertySymbol? x, IPropertySymbol? y) => x?.Name == y?.Name;
    public int GetHashCode(IPropertySymbol obj) => obj.Name.GetHashCode();
}