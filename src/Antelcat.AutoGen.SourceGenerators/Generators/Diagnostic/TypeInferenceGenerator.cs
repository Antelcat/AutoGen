using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class TypeInferenceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attrProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(AutoTypeInferenceAttribute).FullName!,
            (c, t) => true,
            (c, t) => c);
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider((c, t) =>
                c is BaseObjectCreationExpressionSyntax,
            (c, t) => c);
        context.RegisterSourceOutput(syntaxProvider.Collect().Combine(attrProvider.Collect()), (source, tuple) =>
        {
            var (syntax, attrs) = tuple;

            var attr     = attrs.First().GetAttributes<AutoTypeInferenceAttribute>().First();
            var prefixes = attr.Prefixes;
            var suffixes = attr.Suffixes;

            Dictionary<string, DetectedType> types = [];

            foreach (var syntaxContext in syntax)
            {
                var semanticModel      = syntaxContext.SemanticModel;
                var baseObjectCreation = (syntaxContext.Node as BaseObjectCreationExpressionSyntax)!;
                var typeName           = baseObjectCreation.GetCodeTypeName();
                if (typeName is null) continue;

                if (prefixes != null && prefixes.All(x => !typeName.StartsWith(x))) continue;
                if (suffixes != null && suffixes.All(x => !typeName.EndsWith(x))) continue;

                var symbol = ModelExtensions.GetSymbolInfo(semanticModel, baseObjectCreation);
                if (symbol.Symbol is not null) continue;
                var detected = new DetectedType(typeName, semanticModel, baseObjectCreation);
                var init     = baseObjectCreation.Initializer;
                foreach (var assignment in init?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
                {
                    var propName = (assignment.Left as IdentifierNameSyntax)?.Identifier.Text;
                    if (propName is null) continue;
                    var propType = assignment.Right.GetExpressionReturnType(semanticModel);
                    detected.Properties.Add(propName, propType);
                }

                if (types.TryGetValue(detected.FullName, out var exist)) exist.Merge(detected);
                else types.Add(detected.FullName, detected);
            }

            foreach (var type in types.Values)
            {
                try
                {
                    source.AddSource($"AutoTypeInference_{type.FullName}.cs",
                        CompilationUnit()
                            .AddMembers(type.Code(attr))
                            .NormalizeWhitespace().GetText(Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    //
                }
            }
        });
    }
}

[DebuggerDisplay("{ToString()}")]
file class DetectedType
{
    public DetectedType(string typeName, SemanticModel semanticModel, BaseObjectCreationExpressionSyntax detectedNode)
    {
        this.semanticModel = semanticModel;
        var splits = typeName.Replace("global::", "")
            .Split('.')
            .Where(x => x != null && x.Trim() != string.Empty)
            .ToArray();
        if (splits.Length == 1)
        {
            TypeName = splits[0];
        }
        else
        {
            Namespace = string.Join(".", splits.Take(splits.Length - 1));
            TypeName  = splits[^1];
        }

        detectNodes.Add(detectedNode);
    }

    public string FullName => (Namespace is null ? null : Namespace + ".") + TypeName;

    private string? Namespace { get; }

    private string TypeName { get; }

    private readonly List<BaseObjectCreationExpressionSyntax> detectNodes = [];

    private readonly SemanticModel semanticModel;

    public readonly Dictionary<string, ITypeSymbol?> Properties = [];

    public void Merge(DetectedType another)
    {
        detectNodes.AddRange(another.detectNodes);
        foreach (var property in another.Properties)
        {
            if (!Properties.TryGetValue(property.Key, out var exist))
            {
                Properties.Add(property.Key, property.Value);
                continue;
            }

            Properties[property.Key] = ExpressionExtensions.GetSharedParent(exist, property.Value);
        }
    }

    public override string ToString() => FullName + "|" + Properties.Count;

    public MemberDeclarationSyntax Code(AutoTypeInferenceAttribute attribute)
    {
        var typeDeclare = ((TypeDeclarationSyntax)ParseMemberDeclaration(
                attribute.AttributeOnType +
            ((TypeDeclarationSyntax)(attribute.Kind is AutoTypeInferenceAttribute.TypeKind.Class
                ? ClassDeclaration(TypeName)
                : SyntaxContextExtension.RecordDeclaration(TypeName)))
            .NormalizeWhitespace()
            .GetText(Encoding.UTF8))!)
            .AddModifiers(ParseToken(attribute.Accessibility.ToCodeString()))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddMembers(Properties.Select(x =>
                    $"{attribute.AttributeOnProperty} public {(x.Value ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object)).GetFullyQualifiedName()} {x.Key} {{ get; set; }}")
                .Select(x => ParseMemberDeclaration(x)!)
                .ToArray());
        return ((MemberDeclarationSyntax)(Namespace is not null
                ? NamespaceDeclaration(ParseName(Namespace)).AddMembers(typeDeclare)
                : typeDeclare))
            .WithLeadingTrivia(TriviaList([
                Comment($"// <auto-generated/> By {nameof(Antelcat)}.{nameof(AutoGen)}"),
                ..detectNodes.Select(x =>
                {
                    var span = x.GetLocation().GetMappedLineSpan();
                    return Comment(
                        $"// Detected declaration : {Path.GetFileName(span.Path)}, Line:{span.Span.Start.Line}");
                }).ToArray(),
                Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)),
                Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
            ]));
    }

    private string FindLocation(SyntaxNode syntax)
    {
        var sb = new StringBuilder();
        try
        {
            do
            {
                if (syntax is null) return sb.ToString();
                if (syntax is BaseMethodDeclarationSyntax method)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(method);
                    return sb.Insert(0,
                            symbol?.ContainingType.GetFullyQualifiedName() + "." + symbol?.GetFullyQualifiedName())
                        .ToString();
                }

                if (syntax is BaseTypeDeclarationSyntax type)
                {
                    var symbol = semanticModel.GetSymbolInfo(type);
                    sb = sb.Insert(0, (symbol.Symbol as ITypeSymbol)?.GetFullyQualifiedName());
                    return sb.ToString();
                }

                syntax = syntax.Parent;
            } while (true);
        }
        catch
        {
            return sb.ToString();
        }
    }
}

file static class ExpressionExtensions
{
    public static string? GetCodeTypeName(this BaseObjectCreationExpressionSyntax baseObjectCreationExpressionSyntax) =>
        (baseObjectCreationExpressionSyntax switch
        {
            ObjectCreationExpressionSyntax objectCreation => objectCreation.Type.ToFullString(),
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation
                .Parent?.Parent?.Parent is VariableDeclarationSyntax variableDeclaration
                ? variableDeclaration.Type.ToFullString()
                : null,
            _ => null
        })?.Trim().Replace("\r", "").Replace("\n", "");

    public static ITypeSymbol? GetExpressionReturnType(this ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression switch
        {
            BinaryExpressionSyntax bin => GetSharedParent(
                bin.Left.GetExpressionReturnType(semanticModel),
                bin.Right.GetExpressionReturnType(semanticModel)
            ),
            CastExpressionSyntax cast => semanticModel.GetSymbolInfo(cast.Type).Symbol as ITypeSymbol,
            LiteralExpressionSyntax literal => literal.Kind() switch
            {
                SyntaxKind.StringLiteralExpression => semanticModel.Compilation.GetSpecialType(
                    SpecialType.System_String),
                SyntaxKind.NumericLiteralExpression => semanticModel.Compilation.GetSpecialType(
                    SpecialType.System_Int32),
                _ => null
            },

            ConditionalExpressionSyntax conditional =>
                GetSharedParent(
                    conditional.WhenTrue.GetExpressionReturnType(semanticModel),
                    conditional.WhenFalse.GetExpressionReturnType(semanticModel)
                ),
            _ => semanticModel.GetSymbolInfo(expression).Symbol switch
            {
                ILocalSymbol localSymbol       => localSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IFieldSymbol fieldSymbol       => fieldSymbol.Type,
                IMethodSymbol methodSymbol => methodSymbol.MethodKind == MethodKind.Constructor
                    ? methodSymbol.ContainingSymbol as ITypeSymbol
                    : methodSymbol.ReturnType,
                _ => null
            } is { IsAnonymousType: false } type
                ? type
                : null
        };

    public static ITypeSymbol? GetSharedParent(params ITypeSymbol?[] types) =>
        types.Length switch
        {
            0 => null,
            1 => types[0],
            _ => types.Skip(1)
                .Aggregate(types[0],
                    (share, current) =>
                    {
                        if (share is null || current is null) return null;
                        if (SymbolEqualityComparer.Default.Equals(share, current)) return share;

                        var same = GetSame(share, current);
                        if (same != null)
                        {
                            return same;
                        }
                        var b = share.BaseType;
                        if (b != null) // not interface
                        {
                            List<ITypeSymbol> bases = [share];
                            while (b != null && b.SpecialType != SpecialType.System_Object) // dont judge object
                            {
                                bases.Add(b);
                                b = b.BaseType;
                            }

                            var c = current.BaseType;
                            while (c != null && c.SpecialType != SpecialType.System_Object)
                            {
                                if (bases.Contains(c, SymbolEqualityComparer.Default))
                                {
                                    return c;
                                }

                                c = c.BaseType;
                            }
                        }

                        var interfaces = share.AllInterfaces.ToList();
                        foreach (var @interface in current.AllInterfaces)
                        {
                            if (interfaces.Contains(@interface, SymbolEqualityComparer.Default))
                            {
                                return @interface;
                            }
                        }

                        return null;
                    })
        };

    public static INamedTypeSymbol? GetSame(ITypeSymbol symbol, ITypeSymbol another)
    {
        INamedTypeSymbol? nullable = null;
        ITypeSymbol       real;
        if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            nullable  = (symbol as INamedTypeSymbol)!;
            real = nullable.TypeArguments[0];
        }
        else
        {
            real = symbol;
        }

        if (another.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            return nullable != null ? // symbol is nullable
                SymbolEqualityComparer.Default.Equals(real, another) ? nullable : null :
                SymbolEqualityComparer.Default.Equals(symbol, another) ? symbol as INamedTypeSymbol : null;
        var nullable2 = (another as INamedTypeSymbol)!;
        var real2     = nullable2.TypeArguments[0];
        return nullable != null ? // symbol is nullable
            SymbolEqualityComparer.Default.Equals(real, real2) ? nullable : null :
            // symbol is not nullable
            SymbolEqualityComparer.Default.Equals(real, real2) ? nullable2 : null;
    }
}