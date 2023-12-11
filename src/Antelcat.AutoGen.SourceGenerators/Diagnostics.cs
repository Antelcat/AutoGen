using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators;

public static class Diagnostics
{
    public static class Error
    {
        public static DiagnosticDescriptor AM0001(string category) =>
            new(nameof(AM0001),
                "Type not qualified",
                "Type marked with [{0}] can not have {1}",
                category,
                DiagnosticSeverity.Error,
                true);
        
        public static DiagnosticDescriptor AM0002(string category) =>
            new(nameof(AM0002),
                "Method not qualified",
                "Method marked with [{0}] should {1}",
                category,
                DiagnosticSeverity.Error,
                true);
    }
}