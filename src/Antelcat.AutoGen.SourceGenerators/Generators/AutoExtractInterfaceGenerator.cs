using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;
using Type = Feast.CodeAnalysis.CompileTime.Type;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator]
public class AutoExtractInterfaceGenerator : AttributeDetectBaseGenerator<AutoExtractInterfaceAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (initialization, source, compilation, array) = contexts;
        foreach (var syntaxContext in array)
        {
            foreach (var attribute in syntaxContext.GetAttributes())
            {
                var typeNode      = (syntaxContext.TargetNode as TypeDeclarationSyntax)!;
                var symbol        = (syntaxContext.TargetSymbol as INamedTypeSymbol)!;
                var semantic      = syntaxContext.SemanticModel;
                var typeName      = symbol.Name;
                var interfaceName = (attribute.NamingTemplate ?? "I{Name}").Replace("{Name}", typeName);
                var @namespace = attribute.Namespace ?? symbol.GetNamespaceSymbol()?
                    .GetFullyQualifiedName().Replace("global::", "");
                try
                {
                    var usedParams = attribute.ReceiveGeneric?
                        .Select(x => symbol.TypeParameters[x])
                        .ToArray() ?? [];
                    var unUsedParams = symbol.TypeParameters
                        .Where(x => !usedParams.Contains(x, SymbolEqualityComparer.Default))
                        .ToArray();

                    var members = new List<MemberDeclarationSyntax>();
                    foreach (var node in typeNode.ChildNodes())
                    {
                        MemberDeclarationSyntax member;

                        var memberSymbol = node is not EventFieldDeclarationSyntax efd
                            ? semantic.GetDeclaredSymbol(node)
                            : semantic.GetDeclaredSymbol(efd.Declaration.Variables.First());
                        
                        //无法解析的成员 or 不满足条件的成员
                        if (memberSymbol is null || !Qualified(memberSymbol)) continue;
                        //含有类型泛型，但是类型泛型没有传递到接口
                        if (unUsedParams.Any(x => memberSymbol.UsingType(x))) continue;
                        switch (node)
                        {
                            case PropertyDeclarationSyntax prop:
                                if (attribute.Exclude?.Contains(prop.Identifier.Text) is true) continue;
                                var                             propSymbol = (memberSymbol as IPropertySymbol)!;
                                var                             newProp    = prop.FullQualifiedProperty(semantic);
                                List<AccessorDeclarationSyntax> accessors  = [];
                                if (propSymbol.GetMethod?.DeclaredAccessibility is Accessibility.Public)
                                {
                                    accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                                }

                                if (propSymbol.SetMethod?.DeclaredAccessibility is Accessibility.Public)
                                {
                                    accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                                }

                                member = newProp.WithAccessorList(
                                    AccessorList(new SyntaxList<AccessorDeclarationSyntax>(accessors)));
                                break;
                            case MethodDeclarationSyntax method:
                                if (attribute.Exclude?.Contains(method.Identifier.Text) is true) continue;
                                member = method.FullQualifiedMethod(semantic);
                                break;
                            case EventDeclarationSyntax @event:
                                if (attribute.Exclude?.Contains(@event.Identifier.Text) is true) continue;
                                member = @event.FullQualifiedEvent(semantic);
                                break;
                            case EventFieldDeclarationSyntax eventField:
                                if (eventField.Declaration.Variables.Any(x =>
                                    attribute.Exclude?.Contains(x.Identifier.Text) is true))
                                {
                                    continue;
                                }
                               
                                member = eventField.FullQualifiedEvent(semantic);
                                break;
                            default:
                                continue;
                        }

                        members.Add(member);
                    }

                    var declare = InterfaceDeclaration(interfaceName)
                        .AddModifiers(Token(attribute.Accessibility is ComponentModel.Accessibility.Public
                            ? SyntaxKind.PublicKeyword
                            : SyntaxKind.InternalKeyword))
                        .AddMembers(members.ToArray());


                    if (usedParams.Any())
                    {
                        declare = declare
                            .AddTypeParameterListParameters(usedParams.Select((x, i) =>
                                {
                                    var param     = TypeParameter(x.Name);
                                    var constrain = attribute.GetConstrainAt(i);
                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.In))
                                        return param.WithVarianceKeyword(Token(SyntaxKind.InKeyword));

                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Out))
                                        return param.WithVarianceKeyword(Token(SyntaxKind.OutKeyword));

                                    return param;
                                })
                                .ToArray())
                            .AddConstraintClauses(usedParams.Select((x, i) =>
                                {
                                    var constraint = TypeParameterConstraintClause(x.Name);
                                    var constrain  = attribute.GetConstrainAt(i);
                                    var cs         = new List<TypeParameterConstraintSyntax>();
                                    var inherit =
                                        constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Inherit);
                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Unmanaged) ||
                                        (x.HasUnmanagedTypeConstraint && inherit))
                                    {
                                        cs.Add(TypeConstraint(ParseTypeName("unmanaged")));
                                    }

                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Class) ||
                                        (x.HasReferenceTypeConstraint && inherit))
                                    {
                                        cs.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
                                    }

                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Struct) ||
                                        (x is { HasUnmanagedTypeConstraint: false, HasValueTypeConstraint: true } &&
                                         inherit))
                                    {
                                        cs.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));
                                    }

                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.Types) ||
                                        (x.ConstraintTypes.Length > 0 && inherit))
                                    {
                                        foreach (var type in x.ConstraintTypes)
                                        {
                                            cs.Add(TypeConstraint(ParseTypeName(type.GetFullyQualifiedName())));
                                        }
                                    }

                                    if (constrain.HasFlag(AutoExtractInterfaceAttribute.GenericConstrain.New) ||
                                        (x.HasConstructorConstraint && inherit))
                                    {
                                        cs.Add(ConstructorConstraint());
                                    }

                                    return (constraint, cs);
                                })
                                .Where(x => x.cs.Count > 0)
                                .Select(x => x.constraint.WithConstraints(SeparatedList(x.cs.ToArray())))
                                .ToArray());
                    }

                    if (attribute.Interfaces?.Any() is true)
                    {
                        declare = declare.AddBaseListTypes(attribute.Interfaces?.Select((x, i) =>
                        {
                            var baseSymbol = ((x as Type)!.Symbol as INamedTypeSymbol)!;
                            var pass       = attribute.GetPassGenericAt(i);
                            if (!baseSymbol.IsUnboundGenericType
                                || pass is null
                                || pass.Length is 0)
                            {
                                return SimpleBaseType(ParseTypeName(baseSymbol.GetFullyQualifiedName()));
                            }

                            return SimpleBaseType(ParseTypeName(
                                $"{baseSymbol.GetFullyQualifiedName().Split('<')[0]}<{string.Join(",", pass.Select(i => usedParams[i].Name))}>"));
                        }).Cast<BaseTypeSyntax>().ToArray() ?? []);
                    }

                    if (string.IsNullOrWhiteSpace(@namespace))
                    {
                        var comp = CompilationUnit()
                            .AddMembers(declare
                                .WithLeadingTrivia(Header)
                            ).NormalizeWhitespace();
                        source.AddSource("AutoExtractInterface_" + interfaceName.ToQualifiedFileName() + ".cs",
                            comp.GetText(Encoding.UTF8));
                    }
                    else
                    {
                        var comp = CompilationUnit()
                            .AddMembers(NamespaceDeclaration(ParseName(@namespace))
                                .WithLeadingTrivia(Header)
                                .AddMembers(declare)
                            ).NormalizeWhitespace();
                        source.AddSource(
                            "AutoExtractInterface_" + @namespace + "." + interfaceName.ToQualifiedFileName() + ".cs",
                            comp.GetText(Encoding.UTF8));
                    }
                }
                catch (Exception ex)
                {
                    source.AddSource(
                        "AutoExtractInterface_" + @namespace + "." + interfaceName.ToQualifiedFileName() + ".Error.cs",
                        SourceText.From(
                            string.Join("\n",
                                ex.ToString().Split(90).Select(x => $"// {x}")), Encoding.UTF8));
                }
            }
        }
    }

    public static bool Qualified(ISymbol x) => x is
    {
        Kind                 : SymbolKind.Property or SymbolKind.Method or SymbolKind.Event,
        IsStatic             : false,
        IsImplicitlyDeclared : false,
        DeclaredAccessibility: Accessibility.Public
    };
}

file static class Extensions
{
    public static AutoExtractInterfaceAttribute.GenericConstrain GetConstrainAt(
        this AutoExtractInterfaceAttribute attribute, int index)
        => attribute.GenericConstrains?.Length > index
            ? attribute.GenericConstrains[index]
            : AutoExtractInterfaceAttribute.GenericConstrain.Inherit;

    public static int[]? GetPassGenericAt(this AutoExtractInterfaceAttribute attribute, int index)
        => attribute.PassGeneric?.Length > index
            ? attribute.PassGeneric[index].Split(',').Select(int.Parse).ToArray()
            : null;

    public static IEnumerable<AutoExtractInterfaceAttribute> GetAttributes(this GeneratorAttributeSyntaxContext context)
    {
        return context.GetAttributes<AutoExtractInterfaceAttribute>();
        /*yield return new AutoExtractInterfaceAttribute
        {
            ReceiveGeneric = [0, 1, 2]
        };*/
    }

    public static MethodDeclarationSyntax FullQualifiedMethod(this MethodDeclarationSyntax syntax,
                                                              SemanticModel semanticModel)
        => syntax.WithBody(null)
            .WithReturnType(syntax.ReturnType.FullQualifiedType(semanticModel))
            .WithParameterList(ParameterList(
                SeparatedList(
                    syntax.ParameterList.Parameters.Select(x =>
                        x.WithType(x.Type?.FullQualifiedType(semanticModel))).ToArray())))
            .WithBody(null)
            .WithExpressionBody(null)
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword))) //remove partial and async
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    public static PropertyDeclarationSyntax FullQualifiedProperty(this PropertyDeclarationSyntax syntax,
                                                                  SemanticModel semanticModel)
        => syntax.WithType(syntax.Type.FullQualifiedType(semanticModel))
            .WithExpressionBody(null)
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword)))
            .WithInitializer(null)
            .WithSemicolonToken(Token(SyntaxKind.None));

    public static EventDeclarationSyntax FullQualifiedEvent(this EventDeclarationSyntax syntax,
                                                            SemanticModel semanticModel)
        => syntax.WithType(syntax.Type.FullQualifiedType(semanticModel))
            .WithAccessorList(null)
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    public static EventFieldDeclarationSyntax FullQualifiedEvent(this EventFieldDeclarationSyntax syntax,
                                                                 SemanticModel semanticModel) =>
        syntax.WithDeclaration(
                syntax.Declaration.WithType(syntax.Declaration.Type.FullQualifiedType(semanticModel)))
            .WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    public static TypeSyntax FullQualifiedType(this TypeSyntax syntax, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(syntax)
            .Symbol is { } symbol
            ? ParseTypeName(symbol.GetFullyQualifiedName()) : syntax;

    public static INamespaceSymbol? GetNamespaceSymbol(this ISymbol symbol)
    {
        while (symbol is not INamespaceSymbol)
        {
            symbol = symbol.ContainingSymbol;
            if (symbol is IAssemblySymbol)
            {
                return null;
            }
        }

        return symbol as INamespaceSymbol;
    }

    public static bool UsingType(this ISymbol symbol, ITypeSymbol type) => symbol switch
    {
        IMethodSymbol method     => method.UsingType(type),
        IPropertySymbol property => property.UsingType(type),
        IEventSymbol @event      => @event.UsingType(type),
        _                        => throw new ArgumentOutOfRangeException()
    };

    public static bool UsingType(this IEventSymbol @event, ITypeSymbol type) =>
        @event.Type.UsingType(type);

    public static bool UsingType(this IPropertySymbol property, ITypeSymbol generic) =>
        property.Type.UsingType(generic);

    public static bool UsingType(this IMethodSymbol method, ITypeSymbol generic) =>
        method.ReturnType.UsingType(generic) ||
        method.Parameters.Any(x => x.Type.UsingType(generic));

    public static bool UsingType(this ITypeSymbol type, ITypeSymbol generic) =>
        SymbolEqualityComparer.Default.Equals(type, generic) ||
        (type is INamedTypeSymbol namedTypeSymbol &&
         namedTypeSymbol.TypeArguments.Any(x => x.UsingType(generic)));
}