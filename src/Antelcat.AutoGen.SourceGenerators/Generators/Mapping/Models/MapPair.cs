using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;

public record MapPair(string Receiver, 
    string? Provider = null, 
    IMethodSymbol? By = null, 
    bool IsRequired = false)
{
    /// <summary>
    /// "{receiver} = {argName}.{provider}"
    /// </summary>
    /// <param name="argName"></param>
    /// <returns></returns>
    public string Call(string argName) => Provider == null && !IsRequired
        ? string.Empty
        : $"{Receiver} = {(
            Provider is null
                ? "default"
                : By is null
                    ? $"{argName}.{Provider}"
                    : $"{By.Call($"{argName}.{Provider}")}"
        )}";
}