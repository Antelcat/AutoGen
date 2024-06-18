using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

public record MapPair(string Receiver,string? Provider = null,  IMethodSymbol? By = null)
{
    /// <summary>
    /// "{receiver} = {argName}.{provider}"
    /// </summary>
    /// <param name="argName"></param>
    /// <returns></returns>
    public string Call(string argName) => $"{Receiver} = {(
        Provider is null
            ? "default"
            : By is null
                ? $"{argName}.{Provider}"
                : $"{By.Call($"{argName}.{Provider}")}"
    )}";
}