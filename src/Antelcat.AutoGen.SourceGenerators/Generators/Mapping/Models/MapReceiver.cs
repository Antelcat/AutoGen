using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Utils;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapReceiver : MapSide
{
    public MapReceiver(IMethodSymbol method, ITypeSymbol type, bool allowInit = true) : base(method, type)
    {
        ArgName = method.Parameters.Length == 0
            ? "ret"
            : method.Parameters[0].Name == "ret"
                ? "retVal"
                : "ret";
        AvailableProperties = type.GetAllProperties()
            .Where(x =>
            {
                if (x.IsStatic || x.IsReadOnly) return false;
                if (!allowInit && x.IsInitOnly()) return false;
                return x.DeclaredAccessibility.IsIncludedIn(ActualAccess);
            }).Distinct(PropertyNameComparer.Default)
            .ToList();
    }
    
    public override string                       ArgName    { get; }
    public override IEnumerable<IPropertySymbol> AvailableProperties { get; }
    
    
}