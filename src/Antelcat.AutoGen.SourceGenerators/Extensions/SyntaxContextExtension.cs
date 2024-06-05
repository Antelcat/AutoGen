using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class SyntaxContextExtension
{
    public static IEnumerable<T> GetAttributes<T>(this GeneratorAttributeSyntaxContext context) where T : Attribute =>
        context.Attributes.GetAttributes<T>();

    public static IEnumerable<T> GetAttributes<T>(this IEnumerable<AttributeData> attributes) where T : Attribute =>
        attributes.Where(static attributeData =>
                attributeData.AttributeClass?.HasFullyQualifiedMetadataName(typeof(T).FullName) is true)
            .Select(static attributeData => attributeData.ToAttribute<T>());
    
    public static RecordDeclarationSyntax RecordDeclaration(string identifier) =>
        SyntaxFactory.RecordDeclaration(default, default, Token(SyntaxKind.RecordKeyword), identifier: Identifier(identifier), null, null, null, default, SyntaxFactory.Token(SyntaxKind.OpenBraceToken), default, Token(SyntaxKind.CloseBraceToken), default);
}