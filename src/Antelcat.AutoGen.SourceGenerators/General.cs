﻿using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Antelcat.AutoGen.ComponentModel.Abstractions;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Antelcat.AutoGen.SourceGenerators;

internal static class General
{
    internal const string global = nameof(global) + "::";
    
    internal const string Namespace = $"{nameof(Antelcat)}.{nameof(AutoGen)}";

    internal const string ComponentModel = $"{Namespace}.{nameof(ComponentModel)}";

    internal const string GlobalNamespace = "<global namespace>";
    
    internal static string Global(Type? type) => $"{global}{type?.FullName}";
    internal static string Nullable(Type? type) => type?.IsValueType == true ? string.Empty : "?";
    internal static string Generic(string? name) => name             != null ? $"<{name}>" : string.Empty;

    internal static SourceText SourceText(string text) =>
        Microsoft.CodeAnalysis.Text.SourceText.From(text, Encoding.UTF8);

    internal static bool IsValidDeclaration(this string name) => Regex.IsMatch(name, "[a-zA-Z_][a-zA-Z0-9_]*");

    internal static bool IsValidNamespace(this string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var parts = name.Split('.');
        return parts.All(part => part.IsValidDeclaration());
    }

    internal static string HeaderString =>
        CompilationUnit()
            .WithLeadingTrivia(Header)
            .NormalizeWhitespace()
            .GetText(Encoding.UTF8)
            .ToString();
    
    internal static SyntaxTriviaList Header { get; } =
        TriviaList(
            Comment($"// <auto-generated/> By {nameof(Antelcat)}.{nameof(AutoGen)}"),
            Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)));

    private static readonly string GeneratedCode = typeof(GeneratedCodeAttribute).FullName!;

    internal static AttributeListSyntax GeneratedCodeAttribute(Type category) =>
        AttributeList(SingletonSeparatedList(
            Attribute(ParseName(global + GeneratedCode))
                .AddArgumentListArguments(
                    AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(category.FullName!))),
                    AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(Version)))
                )));

    internal static readonly string Version = typeof(AutoGenAttribute).Assembly.GetName().Version.ToString();
    
    private static readonly string ExcludeFromCodeCoverage = typeof(ExcludeFromCodeCoverageAttribute).FullName!;

    internal static AttributeListSyntax ExcludeFromCodeCoverageAttribute() =>
        AttributeList(SingletonSeparatedList(
            Attribute(ParseName(global + ExcludeFromCodeCoverage))));

    internal static MethodDeclarationSyntax AddGenerateAttribute(this MethodDeclarationSyntax syntax, Type category) =>
        syntax.AddAttributeLists(GeneratedCodeAttribute(category), ExcludeFromCodeCoverageAttribute());

    internal static MemberDeclarationSyntax AddGenerateAttribute(this MemberDeclarationSyntax syntax, Type category) =>
        syntax.AddAttributeLists(GeneratedCodeAttribute(category), ExcludeFromCodeCoverageAttribute());

    /// <summary>
    /// 判断某个方法是否允许访问某个成员
    /// </summary>
    /// <param name="method"></param>
    /// <param name="symbol"></param>
    /// <returns></returns>
    internal static bool CanAccess(this IMethodSymbol method, ISymbol symbol)
    {
        if (method.ContainingAssembly.Is(symbol.ContainingAssembly)) return true;
        if (symbol.ContainingType == null) return false;
        var access = GetAccess(method, symbol.ContainingType);
        return symbol.DeclaredAccessibility.IsIncludedIn(access);
    }

    internal static string ToQualifiedFileName(this string className) => className
        .Replace("global::", "")
        .Replace('<', '{')
        .Replace('>', '}');
    
    
    /// <summary>
    /// 获取某类型对类型的访问权
    /// </summary>
    /// <returns></returns>
    internal static Antelcat.AutoGen.ComponentModel.Accessibility GetAccess(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var ret = Antelcat.AutoGen.ComponentModel.Accessibility.Public;
        if (sourceType.Is(targetType.ContainingAssembly))
            ret |= Antelcat.AutoGen.ComponentModel.Accessibility.Internal;
        if (targetType.TypeKind == TypeKind.Interface) return ret;
        var @class = sourceType;
        if (@class.Is(targetType))
        {
            ret |= Antelcat.AutoGen.ComponentModel.Accessibility.Protected |
                   Antelcat.AutoGen.ComponentModel.Accessibility.Private;
        }
        else
        {
            while (@class.BaseType != null)
            {
                @class = @class.BaseType;
                if (!@class.Is(targetType)) continue;
                ret |= Antelcat.AutoGen.ComponentModel.Accessibility.Protected;
                break;
            }
        }

        return ret;
    }
    
    /// <summary>
    /// 获取某方法对类型的访问权
    /// </summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <returns></returns>
    internal static Antelcat.AutoGen.ComponentModel.Accessibility GetAccess(IMethodSymbol method, ITypeSymbol type) => GetAccess(method.ContainingType, type);

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

    internal static Accessibility ToAccessibility(this Antelcat.AutoGen.ComponentModel.Accessibility accessibility) =>
        accessibility switch
        {
            Antelcat.AutoGen.ComponentModel.Accessibility.Public => Accessibility.Public,
            Antelcat.AutoGen.ComponentModel.Accessibility.Private => Accessibility.Private,
            Antelcat.AutoGen.ComponentModel.Accessibility.Internal => Accessibility.Internal,
            Antelcat.AutoGen.ComponentModel.Accessibility.Protected => Accessibility.Protected,
            Antelcat.AutoGen.ComponentModel.Accessibility.ProtectedOrInternal => Accessibility.ProtectedOrInternal,
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null)
        };

    internal static MethodDeclarationSyntax FullQualifiedPartialMethod(this MethodDeclarationSyntax method,
        IMethodSymbol symbol) =>
        method.WithReturnType(ParseName(symbol.ReturnType.GetFullyQualifiedName()))
            .WithParameterList(ParameterList(
                method.ParameterList.Parameters.Aggregate(new SeparatedSyntaxList<ParameterSyntax>(),
                    (l, x) =>
                        l.Add(x.WithType(ParseName(symbol
                                .Parameters[l.Count]
                                .Type
                                .GetFullyQualifiedName()))
                            .WithAttributeLists([])))
            ))
            .WithAttributeLists([])
            .WithSemicolonToken(default);

    internal static ClassDeclarationSyntax PartialClass(this ClassDeclarationSyntax syntax) =>
        ClassDeclaration(syntax.Identifier).WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PartialKeyword)));

    internal static TypeDeclarationSyntax PartialTypeDeclaration(this INamedTypeSymbol @class)
    {
        var identifier = @class.Name + (!@class.IsGenericType
            ? string.Empty
            : $"<{string.Join(",", @class.TypeParameters.Select(x => x.Name))}>");
        return (@class.TypeKind switch
            {
                TypeKind.Class => (@class.IsRecord
                        ? (TypeDeclarationSyntax)SyntaxContextExtension.RecordDeclaration(identifier)
                        : ClassDeclaration(identifier))
                    .AddModifiers(Token(SyntaxKind.PartialKeyword)),
                TypeKind.Interface => InterfaceDeclaration(identifier)
                    .AddModifiers(Token(SyntaxKind.PartialKeyword)),
                TypeKind.Structure or TypeKind.Struct => StructDeclaration(identifier)
                    .AddModifiers(Token(SyntaxKind.PartialKeyword)),
                _ => throw new ArgumentException()
            })
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PartialKeyword)));
    }

    internal static CompilationUnitSyntax AddPartialType(this CompilationUnitSyntax compilationUnit,
        INamedTypeSymbol @class,
        Func<TypeDeclarationSyntax, MemberDeclarationSyntax> map,
        bool leading = true)
    {
        var aggregate = map(@class.PartialTypeDeclaration());
        var parent    = @class.ContainingType;
        while (parent != null)
        {
            aggregate = parent.PartialTypeDeclaration().AddMembers(aggregate);
            if (parent.ContainingType != null) parent = parent.ContainingType;
            else break;
        }

        var namespaceStr = @class.ContainingNamespace.ToDisplayString();
        if (namespaceStr is not GlobalNamespace)
        {
            aggregate = leading
                ? NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
                    .WithLeadingTrivia(Header)
                    .AddMembers(aggregate)
                : NamespaceDeclaration(IdentifierName(@class.ContainingNamespace.ToDisplayString()))
                    .AddMembers(aggregate);
        }
        else if (leading)
        {
            aggregate = aggregate.WithLeadingTrivia(Header);
        }

        return compilationUnit.AddMembers(aggregate);
    }

    public static TypeParameterConstraintClauseSyntax? GetConstraintClause(this Type type)
    {
        if (!type.IsGenericParameter) return null;
        var clause = TypeParameterConstraintClause(type.Name);
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            clause = clause.AddConstraints([ClassOrStructConstraint(SyntaxKind.ClassConstraint)]);
        }

        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            clause = clause.AddConstraints([ClassOrStructConstraint(SyntaxKind.StructConstraint)]);

        }
        else if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            clause = clause.AddConstraints([ConstructorConstraint()]);
        }

        return clause;
    }
}
