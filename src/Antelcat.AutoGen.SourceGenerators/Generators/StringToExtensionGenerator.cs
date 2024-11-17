using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class StringToExtensionGenerator : AttributeDetectBaseGenerator<AutoStringToAttribute>
{
    private const string ClassName = "StringToExtension";

 
    private static MemberDeclarationSyntax[] Content(Compilation compilation)
    {
        var csComp = (compilation as CSharpCompilation)!;
        return csComp
            .References
            .Select(x => csComp.GetAssemblyOrModuleSymbol(x))
            .OfType<IAssemblySymbol>()
            .Aggregate(new List<IMethodSymbol>(), (list, c) =>
            {
                var collector = new TypeCollector(csComp.GetSpecialType(SpecialType.System_String));
                c.GlobalNamespace.Accept(collector);
                list.AddRange(collector.Methods);
                return list;
            })
            .Select(x =>
            {
                var type       = x.Parameters[1].Type;
                var returnName = type.GetFullyQualifiedName();
                var typeName = x.ContainingType.GetFullyQualifiedName();
                var declare =
                    $"""
                     /// <summary>
                     /// Convert from <see cref="string"/> to <see cref="{typeName}"/>
                     /// </summary>
                     {GeneratedCodeAttribute(typeof(StringToExtensionGenerator)).GetText(Encoding.UTF8)}
                     {ExcludeFromCodeCoverageAttribute().GetText(Encoding.UTF8)}
                     [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                     """ +
                    $"public static {returnName} To{type.MetadataName}{GenericDeclaration(x)}(this string? str) {GenericConstrain(x)} => {typeName}.TryParse{GenericDeclaration(x)}(str, out var value) ? value : default;";
                return ParseMemberDeclaration(declare)!;
            })
            .ToArray();
    }

    protected override bool FilterSyntax(SyntaxNode node) => node is CompilationUnitSyntax or ClassDeclarationSyntax;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, compilation, syntaxArray) = contexts;
        var classes = syntaxArray
            .Where(static x => x.TargetNode is ClassDeclarationSyntax)
            .GroupBy(static x => x.TargetSymbol, SymbolEqualityComparer.Default);
        foreach (var group in classes)
        {
            var unit = CompilationUnit()
                .AddMembers(
                    NamespaceDeclaration(IdentifierName(group.Key.ContainingNamespace.ToDisplayString()))
                        .AddMembers(
                            ClassDeclaration(group.Key.Name)
                                .AddModifiers(SyntaxKind.PartialKeyword)
                                .AddMembers(Content(compilation)))
                        .WithLeadingTrivia(Header));
            context.AddSource(group.Key.Name.ToQualifiedFileName("AutoStringTo"), unit
                .NormalizeWhitespace()
                .GetText(Encoding.UTF8));
        }

        var assemblies = syntaxArray
            .Where(static x => x.TargetNode is CompilationUnitSyntax)
            .SelectMany(static x => x.Attributes)
            .Select(static x =>
            {
                var attr = x.ToAttribute<AutoStringToAttribute>();
                return (name: attr.Namespace, access: attr.Accessibility);
            })
            .Where(static x => x.name.IsValidNamespace())
            .GroupBy(static x => x.name);
        foreach (var group in assemblies)
        {
            var name   = group.First().name;
            var access = group.First().access;
            var unit = CompilationUnit()
                .AddMembers(
                    NamespaceDeclaration(IdentifierName(name))
                        .AddMembers(
                            ClassDeclaration(ClassName)
                                .AddModifiers(access switch
                                {
                                    Accessibility.Public => SyntaxKind.PublicKeyword,
                                    _                    => SyntaxKind.InternalKeyword
                                }, SyntaxKind.StaticKeyword)
                                .AddMembers(Content(compilation)))
                        .WithLeadingTrivia(Header));
            context.AddSource($"{name}.{ClassName}.g.cs", unit
                .NormalizeWhitespace()
                .GetText(Encoding.UTF8));
        }
    }

    private static string GenericDeclaration(IMethodSymbol method)
    {
        var ret = string.Join(",", method.TypeParameters.Select(x => x.Name));
        return string.IsNullOrWhiteSpace(ret) ? string.Empty : $"<{ret}>";
    }

    private static string GenericConstrain(IMethodSymbol method)
    {
        return string.Join(" ", method.TypeParameters.Select(x =>
        {
            System.Collections.Generic.List<string> constrains = [];

            if (x.HasReferenceTypeConstraint) constrains.Add("class");
            if (x.HasNotNullConstraint) constrains.Add("notnull");
            if (x.HasValueTypeConstraint) constrains.Add(x.HasUnmanagedTypeConstraint ? "unmanaged" : "struct");
            if (x.HasConstructorConstraint) constrains.Add("new()");
            return constrains.Count == 0 ? string.Empty : $"where {x.Name} : {string.Join(", ", constrains)}";
        }));
    }

    private class TypeCollector(INamedTypeSymbol stringSymbol) : SymbolVisitor
    {
        public List<IMethodSymbol> Methods { get; } = [];

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            var method =
                symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(x => x is
                                         {
                                             IsExtensionMethod: false,
                                             IsStatic         : true,
                                             Name             : "TryParse",
                                             Parameters.Length: 2
                                         }
                                         && SymbolEqualityComparer.Default.Equals(x.Parameters[0].Type, stringSymbol)
                    );
            if (method is not null)
            {
                Methods.Add(method);
            }

            foreach (var member in symbol.GetMembers())
            {
                member.Accept(this);
            }
        }
    }
}