﻿using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antelcat.AutoGen.ComponentModel.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Antelcat.AutoGen.SourceGenerators;

internal static class General
{
    internal const string Namespace = $"{nameof(Antelcat)}.{nameof(AutoGen)}";
    
    internal const string ComponentModel = $"{Namespace}.{nameof(ComponentModel)}";
    internal static string Global(Type? type) => $"global::{type?.FullName}";
    internal static string Nullable(Type? type) => type?.IsValueType == true ? string.Empty : "?";
    internal static string Generic(string? name) => name             != null ? $"<{name}>" : string.Empty;
    internal static SourceText SourceText(string text) =>
        Microsoft.CodeAnalysis.Text.SourceText.From(text, Encoding.UTF8);

    internal static bool IsInvalidDeclaration(this string name) => Regex.IsMatch(name, "[a-zA-Z_][a-zA-Z0-9_]*");

    internal static bool IsInvalidNamespace(this string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var parts = name.Split('.');
        return parts.All(part => part.IsInvalidDeclaration());
    }

    internal static SyntaxTriviaList Header =
        TriviaList(
            Comment($"// <auto-generated/> By {nameof(Antelcat)}.{nameof(AutoGen)}"),
            Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)));

    private static readonly string GeneratedCode = typeof(GeneratedCodeAttribute).FullName!;
    
    internal static AttributeListSyntax GeneratedCodeAttribute(Type category)
    {
        return AttributeList(SingletonSeparatedList(
            Attribute(ParseName("global::" + GeneratedCode))
                .AddArgumentListArguments(
                    AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(category.FullName!))),
                    AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(typeof(AutoGenAttribute).Assembly.GetName().Version.ToString())))
                )));
    }
    private static readonly string ExcludeFromCodeCoverage = typeof(ExcludeFromCodeCoverageAttribute).FullName!;

    internal static AttributeListSyntax ExcludeFromCodeCoverageAttribute()
    {
        return AttributeList(SingletonSeparatedList(
            Attribute(ParseName("global::" + ExcludeFromCodeCoverage))));
    }

    internal static MethodDeclarationSyntax AddGenerateAttribute(this MethodDeclarationSyntax syntax, Type category) =>
        syntax.AddAttributeLists(GeneratedCodeAttribute(category), ExcludeFromCodeCoverageAttribute());

    internal static MemberDeclarationSyntax AddGenerateAttribute(this MemberDeclarationSyntax syntax, Type category) =>
        syntax.AddAttributeLists(GeneratedCodeAttribute(category), ExcludeFromCodeCoverageAttribute());


    internal static bool IsIncludedIn(this Accessibility accessibility,
        ComponentModel.Accessibility targetAccessibility)

        => accessibility switch
        {
            Accessibility.Public    => targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Public),
            Accessibility.Private   => targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Private),
            Accessibility.Internal  => targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Internal),
            Accessibility.Protected => targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Protected),
            Accessibility.ProtectedOrInternal =>
                targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Protected) ||
                targetAccessibility.HasFlag(AutoGen.ComponentModel.Accessibility.Internal),
            _ => false
        };
}