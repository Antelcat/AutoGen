using Antelcat.AutoGen.ComponentModel;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class StringExtension
{
    public static string ToCodeString(this Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public              => "public",
            Accessibility.Protected           => "protected",
            Accessibility.Internal            => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Private             => "private",
            _                                 => ""
        };
}