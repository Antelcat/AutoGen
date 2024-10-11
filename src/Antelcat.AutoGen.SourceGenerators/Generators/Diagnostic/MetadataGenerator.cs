using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Feast.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator(LanguageNames.CSharp)]
public class MetadataGenerator : AttributeDetectBaseGenerator<AutoMetadataFrom>
{
    private static IEnumerable<string> GetPlaceholders(string template)
    {
        foreach (var match in Regex.Matches(template, "{[\\w+((\\.)?)]+}"))
        {
            var str = match.ToString();
            yield return str.Substring(1, str.Length - 2);
        }
    }

    private static StringBuilder Resolve(MemberInfo info, StringBuilder stringBuilder)
    {
        foreach (var placeholder in GetPlaceholders(stringBuilder.ToString()))
        {
            object value = info;
            foreach (var part in placeholder.Split('.'))
            {
                if (value is not MemberInfo memberInfo) continue;
                var val = GetValue(memberInfo, part);
                if (val is null) return stringBuilder;
                value = val;
            }

            stringBuilder = stringBuilder.Replace('{' + placeholder + '}',
                value is Feast.CodeAnalysis.CompileTime.Type compile
                    ? compile.Symbol.GetFullyQualifiedName()
                    : value.ToString());
        }

        return stringBuilder;
    }

    private static object? GetValue(MemberInfo info, string propertyName)
    {
        var type = info.GetType();
        return PropsMap.GetOrAdd(info.GetType(),
                _ =>
                {
                    ConcurrentDictionary<string, PropertyInfo?> ret = [];

                    var prop = type.GetProperty(propertyName);
                    if (prop is null) return ret;
                    ret.TryAdd(propertyName, prop);
                    return ret;
                }).GetOrAdd(propertyName, _ => type.GetProperty(propertyName))?
            .GetValue(info);
    }

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo?>> PropsMap = [];

    protected override bool FilterSyntax(SyntaxNode node) => true;

    private const BindingFlags Flags = BindingFlags.NonPublic |
                                       BindingFlags.Public    |
                                       BindingFlags.Instance  |
                                       BindingFlags.Static;

    private static List<string> Resolve(AutoMetadataFrom metadata)
    {
        List<string> members = [];
        var          target  = metadata.ForType;
        if (metadata.Leading != null) members.Add(metadata.Leading);

        Map(MemberTypes.Field, () => target.GetFields(Flags).Where(x => !x.IsSpecialName));
        Map(MemberTypes.Property, () => target.GetProperties(Flags));
        Map(MemberTypes.Constructor, () => target.GetConstructors(Flags));
        Map(MemberTypes.NestedType, () => target.GetNestedTypes(Flags));
        Map(MemberTypes.Event, () => target.GetEvents(Flags));
        Map(MemberTypes.Method, () => target.GetMethods(Flags).Where(x => !x.IsSpecialName));

        if (metadata.Trailing != null) members.Add(metadata.Trailing);

        return members;

        void Map(MemberTypes memberTypes, Func<IEnumerable<MemberInfo>> memberGetter)
        {
            if (!metadata.MemberTypes.HasFlag(memberTypes)) return;
            members.AddRange(memberGetter()
                .Select(field => Resolve(field, new StringBuilder(metadata.Template))
                    .ToString())
            );
        }
    }

    protected override void Initialize(SourceProductionContext context,
                                       Compilation compilation,
                                       ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var assemblySyntaxContext in syntaxArray
            .Where(x => x.TargetSymbol is IAssemblySymbol)
            .SelectMany(static x => x.GetAttributes<AutoMetadataFrom>())
            .Where(x => x.ForType is Feast.CodeAnalysis.CompileTime.Type { Symbol: INamedTypeSymbol })
            .GroupBy(static x => (x.ForType as Feast.CodeAnalysis.CompileTime.Type)!.Symbol,
                SymbolEqualityComparer.Default))
        {
            var target = (assemblySyntaxContext.Key as INamedTypeSymbol).ToType();
            foreach (var (metadata, index) in assemblySyntaxContext.Select((x, i) => (x, i)))
            {
                var fileName = $"Assembly_From_{target.QualifiedFullFileName()}_{index}.cs";
                var declare  = ParseMemberDeclaration(string.Join(string.Empty, Resolve(metadata)));
                var unit = declare is null
                    ? ParseCompilationUnit(string.Join(string.Empty, Resolve(metadata)))
                    : CompilationUnit().AddMembers(declare);

                var file = unit
                    .WithLeadingTrivia(Header)
                    .NormalizeWhitespace();
                context.AddSource(fileName, file.GetText(Encoding.UTF8));
            }
        }


        foreach (var typeSyntaxContext in syntaxArray
            .Where(static x => x.TargetSymbol is INamedTypeSymbol)
            .GroupBy(static x => (x.TargetSymbol as INamedTypeSymbol)!, SymbolEqualityComparer.Default))
        {
            var @class = (typeSyntaxContext.Key as INamedTypeSymbol)!;
            foreach (var syntaxContext in typeSyntaxContext)
            {
                foreach (var (metadata, index) in syntaxContext.Attributes.GetAttributes<AutoMetadataFrom>()
                    .Select(static (x, i) => (x, i)))
                {
                    var partial = @class.PartialTypeDeclaration() as MemberDeclarationSyntax;
                    var target  = metadata.ForType;
                    var fileName =
                        $"{@class.ToType().QualifiedFullFileName()}_From_{target.QualifiedFullFileName()}_{index}.cs";
                    var text = partial.WithoutTrailingTrivia()
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8).ToString().Trim();
                    var declare = ParseMemberDeclaration($"{text[..^1]}{string.Join("", Resolve(metadata))}}}");

                    var file = CompilationUnit()
                        .AddPartialType(@class, x => declare ?? partial)
                        .NormalizeWhitespace();
                    context.AddSource(fileName, file.GetText(Encoding.UTF8));
                }
            }
        }
    }
}

