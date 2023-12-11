using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator]
public class StringToExtensionGenerator : IIncrementalGenerator
{
    private const string AttributeName = nameof(GenerateStringToAttribute);

    private const string ClassName = "StringToExtension";

    private static string GetGenericConversion(Type? type)
    {
        if (type is null || type.GenericParameterAttributes == GenericParameterAttributes.None) return string.Empty;
        var sb = new StringBuilder($" where {type.Name} :");
        
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            sb.Append(" class,");
        }
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            sb.Append(" struct,");
        }
        else if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            sb.Append(" new(),");
        }

        return sb.Remove(sb.Length - 1, 1).ToString();
    }
    
    private static readonly MemberDeclarationSyntax[] Content = 
        StringExtensions()
        .Select(static x => ParseMemberDeclaration(x)!)
        .ToArray();
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(GenerateStringToAttribute).FullName!,
            static (node, _) => node is CompilationUnitSyntax or ClassDeclarationSyntax,
            static (ctx, t) =>
                (ctx.TargetNode, ctx.Attributes, Context: ctx));
        context.RegisterSourceOutput(context
                .CompilationProvider
                .Combine(provider.Collect()),
            (ctx, t) =>
            {
                var classes = t.Right
                    .Where(static x => x.TargetNode is ClassDeclarationSyntax)
                    .GroupBy(static x => x.Context.TargetSymbol, SymbolEqualityComparer.Default);
                foreach (var group in classes)
                {
                    var unit = CompilationUnit()
                        .AddMembers(
                            NamespaceDeclaration(IdentifierName(group.Key.ContainingNamespace.ToDisplayString()))
                                .AddMembers(
                                    ClassDeclaration(group.Key.Name)
                                        .AddModifiers(SyntaxKind.PartialKeyword)
                                        .AddMembers(Content))
                                .WithLeadingTrivia(Header));
                    ctx.AddSource($"{group.Key.Name}.g.cs", unit
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8));
                }
                var assemblies = t.Right
                    .Where(static x => x.TargetNode is CompilationUnitSyntax)
                    .SelectMany(static x => x.Attributes)
                    .Select(static x =>
                    {
                        var attr = x.ToAttribute<GenerateStringToAttribute>();
                        return (name: attr.Namespace, access: attr.Accessibility);
                    })
                    .Where(static x => x.name.IsInvalidNamespace())
                    .GroupBy(static x => x.name);
                foreach (var group in assemblies)
                {
                    var name = group.First().name;
                    var access = group.First().access;
                    var unit = CompilationUnit()
                        .AddMembers(
                            NamespaceDeclaration(IdentifierName(name))
                                .AddMembers(
                                    ClassDeclaration(ClassName)
                                        .AddModifiers(access switch
                                        {
                                            Accessibility.Public   => SyntaxKind.PublicKeyword,
                                            _ => SyntaxKind.InternalKeyword
                                        }, SyntaxKind.StaticKeyword)
                                        .AddMembers(Content))
                                .WithLeadingTrivia(Header));
                    ctx.AddSource($"{name}.{ClassName}.g.cs", unit
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8));
                }
            });

    }

    private static IEnumerable<string> StringExtensions()
    {
        var contexts = new List<(Type Type, MethodInfo method, Type? GType)>();
        foreach (var type in typeof(string).Assembly.ExportedTypes)
        {
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(static x => x.Name == nameof(int.TryParse) && x.GetParameters().Length == 2);
            if (method is null) continue;
            contexts.Add(method.IsGenericMethod
                ? (type, method, method.GetGenericArguments().First())
                : (type, method, null));
        }

        return contexts
            .Select(static x =>
                $"""

                 /// <summary>
                 /// Convert from <see cref="string"/> to <see cref="{Global(x.Type)}"/>
                 /// </summary>
                 {GeneratedCodeAttribute(typeof(StringToExtensionGenerator)).GetText(Encoding.UTF8)}
                 {ExcludeFromCodeCoverageAttribute().GetText(Encoding.UTF8)}
                 public static {x.GType?.Name ?? Global(x.Type)}{Nullable(x.GType ?? x.Type)} To{x.Type.Name}{Generic(x.GType?.Name)}(this string? str){
                     GetGenericConversion(x.GType)} => {Global(x.Type)}.{nameof(int.TryParse)}{Generic(x.GType?.Name)}(str, out var result) ? result : default;
                 """); 
    }
}
