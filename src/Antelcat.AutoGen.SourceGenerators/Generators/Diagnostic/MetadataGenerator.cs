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
using EventInfo = Feast.CodeAnalysis.CompileTime.EventInfo;
using FieldInfo = Feast.CodeAnalysis.CompileTime.FieldInfo;
using MethodInfo = Feast.CodeAnalysis.CompileTime.MethodInfo;
using TypeInfo = Microsoft.CodeAnalysis.TypeInfo;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator(LanguageNames.CSharp)]
public class MetadataGenerator : AttributeDetectBaseGenerator<AutoMetadataFromAttribute>
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


    private static List<string> Resolve(AutoMetadataFromAttribute metadata)
    {
        List<string> members = [];
        var          target  = metadata.ForType;
        if (metadata.Leading != null) members.Add(metadata.Leading);

        var flags         = metadata.BindingFlags;
        var allowImplicit = metadata.IncludeImplicit;
        Map(MemberTypes.Field, () => target.GetFields(flags));
        Map(MemberTypes.Property, () => target.GetProperties(flags));
        Map(MemberTypes.Constructor, () => target.GetConstructors(flags));
        Map(MemberTypes.Method, () => target.GetMethods(flags));
        Map(MemberTypes.NestedType, () => target.GetNestedTypes(flags));
        Map(MemberTypes.Event, () => target.GetEvents(flags));

        if (metadata.Trailing != null) members.Add(metadata.Trailing);

        return members;

        void Map(MemberTypes memberTypes, Func<IEnumerable<MemberInfo>> memberGetter)
        {
            if (!metadata.MemberTypes.HasFlag(memberTypes)) return;
            members.AddRange(memberGetter()
                .Where(x => Implicit(x, allowImplicit))
                .Select(member => Resolve(member, new StringBuilder(metadata.Template))
                    .ToString())
            );
        }
    }

    private static bool Implicit(MemberInfo member, bool allow) =>
        member switch
        {
            Feast.CodeAnalysis.CompileTime.FieldInfo field       => !field.Symbol.IsImplicitlyDeclared    || allow,
            Feast.CodeAnalysis.CompileTime.PropertyInfo property => !property.Symbol.IsImplicitlyDeclared || allow,
            Feast.CodeAnalysis.CompileTime.ConstructorInfo constructor => !constructor.Symbol.IsImplicitlyDeclared ||
                                                                          allow,
            Feast.CodeAnalysis.CompileTime.MethodInfo method => !method.Symbol.IsImplicitlyDeclared || allow,
            Feast.CodeAnalysis.CompileTime.Type type         => !type.Symbol.IsImplicitlyDeclared   || allow,
            Feast.CodeAnalysis.CompileTime.EventInfo field   => !field.Symbol.IsImplicitlyDeclared  || allow,
            _                                                => false
        };

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, _, syntaxArray) = contexts;
        foreach (var assemblySyntaxContext in syntaxArray
            .Where(x => x.TargetSymbol is IAssemblySymbol)
            .SelectMany(static x => x.GetAttributes<AutoMetadataFromAttribute>())
            .Where(x => x.ForType is Feast.CodeAnalysis.CompileTime.Type { Symbol: INamedTypeSymbol })
            .GroupBy(static x => (x.ForType as Feast.CodeAnalysis.CompileTime.Type)!.Symbol,
                SymbolEqualityComparer.Default))
        {
            var target = (assemblySyntaxContext.Key as INamedTypeSymbol).ToType();
            foreach (var (metadata, index) in assemblySyntaxContext.Select((x, i) => (x, i)))
            {
                var fileName = $"AutoMetadataFrom__Assembly_From_{target.QualifiedFullFileName()}_{index}.g.cs";
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
                foreach (var (metadata, index) in syntaxContext.Attributes.GetAttributes<AutoMetadataFromAttribute>()
                    .Select(static (x, i) => (x, i)))
                {
                    MemberDeclarationSyntax partial = @class.PartialTypeDeclaration();

                    var target = metadata.ForType;
                    var fileName =
                        $"AutoMetadataFrom__{@class.ToType().QualifiedFullFileName()}_From_{target.QualifiedFullFileName()}_{index}.g.cs";
                    var text = partial.WithoutTrailingTrivia()
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8)
                        .ToString()
                        .Trim();
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

