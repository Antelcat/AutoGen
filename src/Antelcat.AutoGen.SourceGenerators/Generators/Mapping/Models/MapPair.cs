using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

public record MapPair(string Provider, string Receiver, IMethodSymbol? By)
{
    public string Call(string argName) => $"{Receiver} = {(
        By is null
            ? $"{argName}.{Receiver}"
            : $"{By.Call($"{argName}.{Provider}")}"
    )},";
}