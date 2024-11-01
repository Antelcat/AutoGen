using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class TypeInferenceGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attrProvider = context.SyntaxProvider.ForAttributeWithMetadataName(typeof(AutoTypeInferenceAttribute).FullName!,
            (c, t) => true,
            (c, t) => c);
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider((c, t) =>
                c is BaseObjectCreationExpressionSyntax,
            (c, t) => c);
        context.RegisterSourceOutput(attrProvider.Collect().Combine(syntaxProvider.Collect()), ( source, tuple) =>
        {
            var (attr, syntax) = tuple;
            foreach (var syntaxContext in syntax)
            {
                var baseObjectCreation = (syntaxContext.Node as BaseObjectCreationExpressionSyntax)!;
                var symbol             = ModelExtensions.GetSymbolInfo(syntaxContext.SemanticModel, baseObjectCreation);
                var typeName           = baseObjectCreation.GetCodeTypeName();
                var init               = baseObjectCreation.Initializer;
                foreach (var assignment in init?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
                {
                    var name  = (assignment.Left as IdentifierNameSyntax)?.Identifier.Text;
                    var right = syntaxContext.SemanticModel.GetSymbolInfo(assignment.Right);
                    var type = assignment.Right.GetExpressionReturnType(syntaxContext.SemanticModel);
                }
            }
        });
    }
}

file record Region
{
    
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

    private static ITypeSymbol? GetSharedParent(params ITypeSymbol?[] types) =>
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
}