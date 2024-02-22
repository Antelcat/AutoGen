﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ReSharper disable StringLiteralTypo

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class AutoDeconstructGenerator : AttributeDetectBaseGenerator<AutoDeconstructIndexableAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => node is CompilationUnitSyntax;

    protected override void Initialize(SourceProductionContext context, Compilation compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        var syntax = syntaxArray.First();
        var data = syntax.Attributes.FirstOrDefault(x =>
            x.AttributeClass.GetFullyQualifiedName() == global + AttributeName);
        if (data is null) return;
        var attr = data.ToAttribute<AutoDeconstructIndexableAttribute>();
        if (!attr.Namespace.IsValidNamespace()) return;
        var extra = attr.IndexableTypes
            .Select(x =>
            {
                var type = typeof(List<object>);
                if (x.IsGenericType)
                {
                    if (x.GenericParameterCount() is not 1)
                    {
                        return null;
                    }

                    if (!x.IsConstructedGenericType)
                    {
                        x = x.GetGenericTypeDefinition();
                    }
                }

                var prop = x
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(static p =>
                        p is
                        {
                            Name: "Item", GetMethod: var get
                        }
                        && get.GetParameters() is { Length: 1 } param
                        && (param[0].ParameterType.Equals(typeof(int)) ||
                            param[0].ParameterType.Equals(typeof(long))));

                if (prop == null) return null;
                var nullable = ((Feast.CodeAnalysis.CompileTime.PropertyInfo)prop).HasNullableAnnotation;
                if (!x.IsGenericType)
                {
                    return Deconstructs(x.QualifiedFullName(), attr.Size, nullable);
                }

                var element = prop.PropertyType;
                return Deconstructs(x.GlobalQualifiedFullName(),
                    attr.Size,
                    nullable,
                    element.GlobalQualifiedFullName(),
                    element.IsGenericParameter,
                    true,
                    element.GetConstraintClause()?
                        .NormalizeWhitespace()
                        .ToFullString() ?? "");
            });

        var unit = CompilationUnit()
            .AddMembers(
                NamespaceDeclaration(ParseName(attr.Namespace))
                    .AddMembers(
                        ClassDeclaration("DeconstructIndexableExtension")
                            .AddModifiers(SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword)
                            .AddMembers(
                            [
                                ..Deconstructs(global + typeof(IList).FullName, attr.Size),
                                ..Deconstructs(global + typeof(IList<>).QualifiedFullName(),
                                    attr.Size,
                                    false,
                                    "T",
                                    true,
                                    true),
                                ..extra.SelectMany(x => x).ToArray()
                            ])));

        context.AddSource($"{attr.Namespace}.DeconstructIndexableExtension.cs",
            SourceText(unit.NormalizeWhitespace().ToFullString()));
    }

    private const string Prefix = "public static void Deconstruct";

    private static MemberDeclarationSyntax[] Deconstructs(string className,
        int count,
        bool nullable = false,
        string elementType = "object",
        bool isGenericMethod = false,
        bool isGeneric = false,
        string constraint = "") =>
        Enumerable.Range(2, count - 1)
            .Select(x => Deconstruct(className, x, nullable, elementType, isGenericMethod, isGeneric, constraint))
            .ToArray();

    private static MemberDeclarationSyntax Deconstruct(string className,
        int count,
        bool nullable = false,
        string elementType = "object",
        bool isGenericMethod = false,
        bool isGeneric = false,
        string constraint = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"{Prefix}{(isGenericMethod ? $"<{elementType}>" : "")}(this {className}{(isGeneric ? $"<{elementType}>" : "")} list, {
                string.Join(", ", Enumerable.Range(0, count).Select(x => $"out {elementType}{(nullable ? "?" : "")} item{x}"))
            }){constraint}");
        sb.AppendLine("{");
        foreach (var i in Enumerable.Range(0, count))
        {
            sb.AppendLine($"    item{i} = list[{i}];");
        }

        sb.AppendLine("}");
        return ParseMemberDeclaration(sb.ToString())!;
    }
}