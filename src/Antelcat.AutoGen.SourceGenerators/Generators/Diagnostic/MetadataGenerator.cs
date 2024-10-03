using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
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
                    ConcurrentDictionary<string, PropertyInfo?> ret  = [];
                    var                                         prop = type.GetProperty(propertyName);
                    if (prop is null) return ret;
                    ret.TryAdd(propertyName, prop);
                    return ret;
                }).GetOrAdd(propertyName, _ => type.GetProperty(propertyName))?
            .GetValue(info);
    }

    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo?>> PropsMap = [];

    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(SourceProductionContext context,
                                       Compilation compilation,
                                       ImmutableArray<GeneratorAttributeSyntaxContext> syntaxArray)
    {
        foreach (var groupedSyntaxContext in syntaxArray.GroupBy(x => (x.TargetSymbol as INamedTypeSymbol)!,
            SymbolEqualityComparer.Default))
        {
            var @class = (groupedSyntaxContext.Key as INamedTypeSymbol)!;
            foreach (var syntaxContext in groupedSyntaxContext)
            {
                foreach (var (metadata, index) in syntaxContext.Attributes.GetAttributes<AutoMetadataFrom>()
                    .Select((x, i) => (x, i)))
                {
                    var          partial = @class.PartialTypeDeclaration() as MemberDeclarationSyntax;
                    List<string> members = [];
                    if (metadata.Leading != null) members.Add(metadata.Leading);
                    var target = metadata.ForType;
                    var fileName =
                        $"{@class.ToType().QualifiedFullFileName()}_From_{target.QualifiedFullFileName()}_{index}.cs";

                    const BindingFlags flags = BindingFlags.NonPublic |
                                               BindingFlags.Public    |
                                               BindingFlags.Instance  |
                                               BindingFlags.Static;
                    Map(MemberTypes.Field, () => target.GetFields(flags).Where(x => !x.IsSpecialName));
                    Map(MemberTypes.Property, () => target.GetProperties(flags));
                    Map(MemberTypes.Constructor, () => target.GetConstructors(flags));
                    Map(MemberTypes.NestedType, () => target.GetNestedTypes(flags));
                    Map(MemberTypes.Event, () => target.GetEvents(flags));
                    Map(MemberTypes.Method, () => target.GetMethods(flags).Where(x => !x.IsSpecialName));

                    if (metadata.Trailing != null) members.Add(metadata.Trailing);

                    var text = partial.WithoutTrailingTrivia()
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8).ToString().Trim();
                    var declare = ParseMemberDeclaration($"{text[..^1]}{string.Join("", members)}}}");

                    var file = CompilationUnit()
                        .AddPartialType(@class, x => declare ?? partial)
                        .NormalizeWhitespace();
                    context.AddSource(fileName, file.GetText(Encoding.UTF8));
                    continue;

                    void Map(MemberTypes memberTypes, Func<IEnumerable<MemberInfo>> memberGetter)
                    {
                        if (!metadata.MemberTypes.HasFlag(memberTypes)) return;
                        members.AddRange(memberGetter()
                            .Select(field => Resolve(field, new StringBuilder(metadata.Template))
                                .ToString())
                        );
                    }
                }
            }
        }
    }
}