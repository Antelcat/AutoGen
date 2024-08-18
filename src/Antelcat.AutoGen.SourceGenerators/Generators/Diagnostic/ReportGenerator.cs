using Antelcat.AutoGen.ComponentModel.Diagnostic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator(LanguageNames.CSharp)]
public class ReportGenerator : IIncrementalGenerator
{
    public static readonly string MemberKind =
        $"global::{typeof(MemberTypes).FullName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(AutoReport).FullName!,
            (ctx, _) => ctx is MethodDeclarationSyntax,
            (ctx, _) => ctx
        );

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
                                     (ctx, tuple) =>
                                     {
                                         foreach (var group in tuple.Right
                                                      .GroupBy(static x =>
                                                                   (x.TargetSymbol as IMethodSymbol)!.ContainingType,
                                                               SymbolEqualityComparer.Default))
                                         {
                                             var @class  = (group.Key as INamedTypeSymbol)!;
                                             var partial = @class.PartialTypeDeclaration();
                                             foreach (var syntax in group)
                                             {
                                                 var method = (syntax.TargetSymbol as IMethodSymbol)!;
                                                 if (!FilterMethod(method, out var target, out var arg, out var report))
                                                     continue;

                                                 var access = GetAccess(method, target);
                                                 var sb     = new List<StatementSyntax>();
                                                 foreach (var member in target.GetMembers()
                                                              .Where(x => x.DeclaredAccessibility.IsIncludedIn(access)))
                                                 {
                                                     switch (member)
                                                     {
                                                         case IFieldSymbol { IsImplicitlyDeclared: false } field:
                                                             sb.Add(Method(field.MetadataName, field.Type,
                                                                           MemberTypes.Property,
                                                                           $"() => {arg}.{field.Name}"));
                                                             break;
                                                         case IPropertySymbol property:
                                                         {
                                                             if (!property.IsWriteOnly)
                                                             {
                                                                 sb.Add(Method(property.MetadataName, property.Type,
                                                                               MemberTypes.Property,
                                                                               $"() => {arg}.{property.Name}"));
                                                             }

                                                             break;
                                                         }
                                                         case IMethodSymbol methodSymbol:
                                                         {
                                                             if (methodSymbol is
                                                                 {
                                                                     MethodKind       : MethodKind.Ordinary,
                                                                     Parameters.Length: 0
                                                                 })
                                                             {
                                                                 sb.Add(Method(methodSymbol.MetadataName,
                                                                               methodSymbol.ReturnType,
                                                                               MemberTypes.Method,
                                                                               methodSymbol.ReturnsVoid
                                                                                   ? "null"
                                                                                   : $"() => {arg}.{methodSymbol.Name}()"));
                                                             }

                                                             break;
                                                         }
                                                         case INamedTypeSymbol typeSymbol:
                                                         {
                                                             if (typeSymbol.TypeKind == TypeKind.Class)
                                                             {
                                                                 sb.Add(Method(typeSymbol.MetadataName, typeSymbol,
                                                                               MemberTypes.NestedType,
                                                                               $"() => typeof({typeSymbol.GetFullyQualifiedName()})"));
                                                             }

                                                             break;
                                                         }
                                                     }

                                                     StatementSyntax Method(
                                                         string name, ITypeSymbol type, MemberTypes types,
                                                         string getter)
                                                     {
                                                         return ParseStatement(
                                                             $"{report}(\"{name}\", typeof({type.GetFullyQualifiedName()}), {MemberKind}.{types.ToString()}, {getter});");
                                                     }
                                                 }

                                                 var block = (syntax.TargetNode as MethodDeclarationSyntax)!
                                                     .FullQualifiedPartialMethod(method)
                                                     .AddGenerateAttribute(typeof(ReportGenerator))
                                                     .WithBody(
                                                         Block(sb));
                                                 partial = partial.AddMembers(block);
                                             }

                                             ctx.AddSource($"{nameof(AutoReport)}__{@class.GetFullyQualifiedName()
                                                 .ToQualifiedFileName()}.g.cs",
                                                           CompilationUnit()
                                                               .AddPartialType(@class, x => partial)
                                                               .NormalizeWhitespace()
                                                               .GetText(Encoding.UTF8));
                                             return;
                                         }
                                     });
    }

    private static bool FilterMethod(IMethodSymbol method,
                                     [NotNullWhen(true)] out ITypeSymbol? type,
                                     [NotNullWhen(true)] out string? argName,
                                     [NotNullWhen(true)] out string? reportName)
    {
        if (!method.ReturnsVoid) goto failed;

        switch (method)
        {
            case { IsStatic: false, Parameters.Length: 1 }:
                type       = method.ContainingType;
                argName    = "this";
                reportName = method.Parameters[0].Name;
                return true;
            case { IsStatic: true, Parameters.Length: 2 }:
                type       = method.Parameters[0].Type;
                argName    = method.Parameters[0].Name;
                reportName = method.Parameters[1].Name;
                return true;
        }

        failed :
        type       = null;
        argName    = null;
        reportName = null;
        return false;
    }
}

/*[Generator(LanguageNames.CSharp)]*/
public class FinReportGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new Receiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        foreach (var symbol in (context.SyntaxReceiver as Receiver)?.Types(context.Compilation) ?? [])
        {
            if (symbol is not IMethodSymbol methodSymbol) continue;
            if (!methodSymbol.TryGetAttributeWithFullyQualifiedMetadataName(typeof(AutoReport).FullName, out _))
                continue;
            var type = methodSymbol.ContainingType;
            var c = CompilationUnit().AddPartialType(type, x =>
            {
                return x.AddMembers(ParseMemberDeclaration(
                                        $$"""
                                        public void Fun(){
                                            {{
                                                string.Join("\n", type.GetAllMembers()
                                                                .OfType<IPropertySymbol>()
                                                                .Select(p=> $"_ = {p.MetadataName};").ToArray())
                                            }}
                                        }         
                                        """
                                    )!);
            });
            try
            {
                context.AddSource("AutoFinReport__" + type.GetFullyQualifiedName().ToQualifiedFileName(),
                                  SourceText(c.NormalizeWhitespace().ToString()));
            }
            catch
            {
                Debugger.Launch();
            }
        }
    }

    private class Receiver : ISyntaxReceiver
    {
        private readonly IList<MethodDeclarationSyntax> types = [];
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not MethodDeclarationSyntax methodDeclaration) return;
            types.Add(methodDeclaration);
        }

        public IEnumerable<ISymbol?> Types(Compilation compilation) =>
            types.Select(x => compilation.GetSemanticModel(x.SyntaxTree).GetDeclaredSymbol(x));
    }
}