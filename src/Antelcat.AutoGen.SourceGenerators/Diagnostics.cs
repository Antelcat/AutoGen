using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators;

public static class Diagnostics
{
    public static class Error
    {
        public static DiagnosticDescriptor AA0001(string category) =>
            new(nameof(AA0001),
                "Type not qualified",
                "Type marked with [{0}] can not have {1}",
                category,
                DiagnosticSeverity.Error,
                true);
    }
}