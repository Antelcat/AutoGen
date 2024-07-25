using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

internal abstract record MapSide(IMethodSymbol Method, ITypeSymbol Type)
{
    public          Accessibility ActualAccess   { get; } = GetAccess(Method, Type);
    public required Accessibility RequiredAccess { get; init; }

    public abstract string                        ArgName             { get; }
    public required ImmutableArray<AttributeData> Attributes          { get; init; }
    public abstract IEnumerable<IPropertySymbol>  AvailableProperties { get; }

    public IEnumerable<IPropertySymbol> RequiredProperties =>
        AvailableProperties.Where(x =>
            x.DeclaredAccessibility.IsIncludedIn(RequiredAccess) &&
            !ConfigInfo.Excludes.Contains(x.Name));

    public IEnumerable<(string Name, bool IsRequired)> AvailablePropertyNames => RequiredProperties
        .Select(x => (x.Name, x.IsRequired))
        .Concat(ConfigInfo.Includes.Select(x => (x, true)));
    
    public MapConfigInfo ConfigInfo => new(Attributes);
}