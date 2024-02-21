using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Feast.CodeAnalysis.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PropertyInfo = Feast.CodeAnalysis.CompileTime.PropertyInfo;

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
        Debugger.Launch();

        var extra = attr.IndexableTypes
            .Where(x =>
            {
                if (
                    x.GenericParameterCount() is 1 &&
                    ((Type)x).Symbol.GetMembers().OfType<IPropertySymbol>().ToArray().Length > 0)
                {
                }
             

                return x.GenericParameterCount() is 0 or 1 &&
                       x.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p =>
                           p is
                           {
                               Name: "Item", GetMethod: var get
                           }
                           && get.GetParameters() is { Length: 1 } param
                           && (param[0].ParameterType.Equals(typeof(int)) ||
                               param[0].ParameterType.Equals(typeof(long))));
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
                                ..Deconstructs(global + typeof(IList<>).QualifiedFullName(), attr.Size, true),
                                ..extra.SelectMany(x =>
                                        Deconstructs(x.QualifiedFullName(), attr.Size, x.ContainsGenericParameters))
                                    .ToArray()
                            ])));

        context.AddSource($"{attr.Namespace}.DeconstructIndexableExtension.cs",
            SourceText(unit.NormalizeWhitespace().ToFullString()));
    }

    private const string Prefix = "public static void Deconstruct";

    private static MemberDeclarationSyntax[] Deconstructs(string className, int count, bool isGeneric = false) =>
        Enumerable.Range(2, count - 1)
            .Select(x => Deconstruct(className, x, isGeneric))
            .ToArray();

    private static MemberDeclarationSyntax Deconstruct(string className, int count, bool isGeneric = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"{Prefix}{(isGeneric ? "<T>" : "")}(this {className}{(isGeneric ? "<T>" : "")} list, {
                string.Join(", ", Enumerable.Range(0, count).Select(x => $"out {(isGeneric ? "T" : "object")}? item{x}"))
            })");
        sb.AppendLine("{");
        foreach (var i in Enumerable.Range(0, count))
        {
            sb.AppendLine($"    item{i} = list[{i}];");
        }

        sb.AppendLine("}");
        return ParseMemberDeclaration(sb.ToString())!;
    }
}