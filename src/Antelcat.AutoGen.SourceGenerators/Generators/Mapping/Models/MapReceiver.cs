using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal record MapReceiver : MapSide
{
    public MapReceiver(IMethodSymbol method, ITypeSymbol type) : base(method, type)
    {
        ArgName = method.Parameters.Length == 0
            ? "ret"
            : method.Parameters[0].Name == "ret"
                ? "retVal"
                : "ret";
        AvailableProperties = type.GetAllProperties()
            .Where(x =>
                !x.IsStatic && !x.IsReadOnly &&
                x.DeclaredAccessibility.IsIncludedIn(ActualAccess)).ToList();
    }

    public override string                       ArgName    { get; }
    public override IEnumerable<IPropertySymbol> AvailableProperties { get; }
}