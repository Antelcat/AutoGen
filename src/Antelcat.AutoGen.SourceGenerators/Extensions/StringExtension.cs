using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    public static IEnumerable<string> Split(this string str, int max)
    {
        while (str.Length > max)
        {
            yield return str[..max];
            str = str[max..];
        }

        yield return str;
    }
}