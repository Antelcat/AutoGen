using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Feast.CodeAnalysis.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class ExtendForGenerator : AttributeDetectBaseGenerator<AutoExtendForAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) =>
        node is CompilationUnitSyntax ||
        node is ClassDeclarationSyntax classDeclaration          &&
        classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) &&
        classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, compilation, syntaxArray) = contexts;

        foreach (var attributes in syntaxArray
                     .Where(static x => x.TargetNode is CompilationUnitSyntax)
                     .SelectMany(static x => x.GetAttributes<AutoExtendForAttribute>())
                     .Where(FilterAttributes)
                     .GroupBy(static x => ((x.Type as Type)!.Symbol as INamedTypeSymbol)!,
                         SymbolEqualityComparer.Default))
        {
            var className   = attributes.Key!.Name + "Extension";
            var target      = (attributes.Key as INamedTypeSymbol)!;
            var canInternal = compilation.Assembly.Is(target.ContainingAssembly);

            foreach (var attrs in attributes.GroupBy(static x => x.Namespace))
            {
                var total = attrs.FirstOrDefault(static x => x.Methods == null || x.Methods.Length is 0);
                var classDeclare = ClassDeclaration(className)
                    .AddModifiers(SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword);

                if (total != null)
                {
                    classDeclare = Filter(target, canInternal)
                        .Select(GenerateMethod)
                        .Aggregate(classDeclare, static (c, m) => c.AddMembers(m));
                }
                else
                {
                    classDeclare = attributes.Where(static x => x.Methods != null)
                        .SelectMany(x => x.Methods)
                        .Aggregate(classDeclare, (current, method) => Filter(target, canInternal)
                            .Where(x => x.Name == method)
                            .Select(GenerateMethod)
                            .Aggregate(current, static (c, m) => c.AddMembers(m)));
                }

                var unit = CompilationUnit()
                    .AddMembers(NamespaceDeclaration(ParseName(attrs.Key))
                        .AddMembers(classDeclare)
                        .WithLeadingTrivia(Header));
                context.AddSource($"{attrs.Key}.{className}".ToQualifiedFileName("AutoExtendFor"),
                    SourceText(unit.NormalizeWhitespace().ToFullString()));
            }
        }

        foreach (var syntaxContext in syntaxArray
                     .Where(static x => x.TargetNode is ClassDeclarationSyntax))
        {
            var className   = syntaxContext.TargetSymbol.Name;
            var nameSpace   = syntaxContext.TargetSymbol.ContainingNamespace?.ToDisplayString();
            var canInternal = compilation.Assembly.Is(syntaxContext.TargetSymbol.ContainingAssembly);
            var classDeclare = ClassDeclaration(className)
                .AddModifiers(SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword);
            foreach (var attrs in syntaxContext
                         .GetAttributes<AutoExtendForAttribute>()
                         .GroupBy(static x => ((x.Type as Type)!.Symbol as INamedTypeSymbol)!,
                             SymbolEqualityComparer.Default))
            {
                var target = (attrs.Key as INamedTypeSymbol)!;
                var total  = attrs.FirstOrDefault(static x => x.Methods == null || x.Methods.Length is 0);
                if (total != null)
                {
                    classDeclare = Filter(target, canInternal)
                        .Select(GenerateMethod)
                        .Aggregate(classDeclare, static (c, m) => c.AddMembers(m));
                }
                else
                {
                    classDeclare = attrs.Where(static x => x.Methods != null)
                        .SelectMany(x => x.Methods)
                        .Aggregate(classDeclare, (current, method) => Filter(target, canInternal)
                            .Where(x => x.Name == method)
                            .Select(GenerateMethod)
                            .Aggregate(current, static (c, m) => c.AddMembers(m)));
                }

                var unit = CompilationUnit();
                if (nameSpace != null)
                {
                    unit = unit.AddMembers(NamespaceDeclaration(ParseName(nameSpace)).AddMembers(classDeclare)
                        .WithLeadingTrivia(Header));
                }
                else
                {
                    unit = unit.AddMembers(classDeclare.WithLeadingTrivia(Header));
                }

                context.AddSource($"{nameSpace}.{className}".ToQualifiedFileName("AutoExtendFor"),
                    SourceText(unit.NormalizeWhitespace().ToFullString()));
            }
        }
    }

    private static MemberDeclarationSyntax GenerateMethod(IMethodSymbol method)
    {
        var generics = !method.IsGenericMethod
            ? ""
            : $"<{string.Join(",", method.TypeParameters.Select(static x => x.GetFullyQualifiedName()))}>";
        return ParseMemberDeclaration(
            $$"""
              {{(method.DeclaredAccessibility == Accessibility.Public ? "public" : "internal")}} static {{
                  method.ReturnType.GetFullyQualifiedName()
              }}{{(method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "")}} {{
                  method.Name
              }}{{generics}}({{
                  string.Join(", ", method.Parameters.Select(static (x, c) => $"{(c is 0 ? "this " : string.Empty)
                  }{ParamConv(x)}{x.Type.GetFullyQualifiedName()}{(x.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "")} {x.Name}"))
              }}) => {{method.ContainingType.GetFullyQualifiedName()}}.{{method.Name}}{{generics}}({{
                  string.Join(", ", method.Parameters.Select(static x => $"{ParamConv(x, true)}{x.Name}"))
              }});
              """
        )!;

        static string ParamConv(IParameterSymbol symbol, bool call = false) =>
            symbol.RefKind switch
            {
                RefKind.Ref         => "ref ",
                RefKind.Out         => "out ",
                RefKind.RefReadOnly => $"ref {(call ? "" : "readonly")} ",
                _                   => "",
            };
    }

    private static bool FilterAttributes(AutoExtendForAttribute attribute) =>
        attribute.Namespace.IsValidNamespace() &&
        attribute.Type is Type
        {
            Symbol: INamedTypeSymbol { IsStatic: true, IsGenericType: false }
        };
    
    private static IEnumerable<IMethodSymbol> Filter(INamespaceOrTypeSymbol symbol, bool canInternal) =>
        symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x =>
                x.Parameters.Length > 1
                && x.Parameters[0] is not { Type.IsReferenceType: true, RefKind: RefKind.Ref } &&
                (x.DeclaredAccessibility is Accessibility.Public ||
                 canInternal && x.DeclaredAccessibility is Accessibility.Internal));
}
