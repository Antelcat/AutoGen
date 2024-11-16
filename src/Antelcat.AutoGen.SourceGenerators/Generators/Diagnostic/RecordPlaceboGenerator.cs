using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Diagnostic;

[Generator]
public class RecordPlaceboGenerator : IIncrementalGenerator
{
    private static readonly MemberDeclarationSyntax ToStringMethod = ParseMemberDeclaration(
        $"""
         /// <summary>
         /// Placebo by <see cref="{typeof(AutoRecordPlaceboAttribute).FullName}"/>
         /// </summary>
         /// <returns><see cref="object.{nameof(GetType)}"/>.<see cref="{nameof(ToString)}"/></returns>
         public override string {nameof(ToString)}() => {nameof(GetType)}().{nameof(ToString)}();
         """)!;

    private static readonly MemberDeclarationSyntax GetHashCodeMethod = ParseMemberDeclaration(
        $"""
         /// <summary>
         /// Placebo by <see cref="{typeof(AutoRecordPlaceboAttribute).FullName}"/>
         /// </summary>
         /// <returns>base.<see cref="{nameof(GetHashCode)}"/></returns>
         public override int {nameof(GetHashCode)}() => base.{nameof(GetHashCode)}();
         """)!;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(typeof(AutoRecordPlaceboAttribute).FullName!,
                (_, _) => true,
                (a, _) => a);

        var records = context.SyntaxProvider
            .CreateSyntaxProvider((n, _) => n is RecordDeclarationSyntax rec &&
                                            rec.Modifiers.Any(x => x.Text == "partial"),
                (n, _) => n);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect().Combine(records.Collect())),
            (source, triple) =>
            {
                var (compilation, tuple) = triple;
                var (attrs, record)      = tuple;
                if (!attrs.Any()) return;
                foreach (var cs in record
                    .GroupBy(static x => x.SemanticModel.GetDeclaredSymbol(x.Node), SymbolEqualityComparer.Default))
                {
                    var symbol  = (cs.Key as INamedTypeSymbol)!;
                    var members = new List<MemberDeclarationSyntax>();
                    var methods = cs.SelectMany(x =>
                            (x.Node as RecordDeclarationSyntax)!
                            .ChildNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Select(m => (x.SemanticModel.GetDeclaredSymbol(m) as IMethodSymbol)!))
                        .ToList();
                    if (!methods.Any(static m => m is { Name: nameof(GetHashCode), Parameters.Length: 0 })) 
                        members.Add(GetHashCodeMethod);
                    
                    if (!methods.Any(static m => m is { Name: nameof(ToString), Parameters.Length: 0 })) 
                        members.Add(ToStringMethod);
                    
                    if(members.Count is 0) continue;

                    var comp = CompilationUnit()
                        .AddPartialType(symbol, c => c.AddMembers(members.ToArray()))
                        .NormalizeWhitespace()
                        .GetText(Encoding.UTF8);

                    source.AddSource(
                        "AutoRecordPlacebo_" + symbol.GetFullyQualifiedName().ToQualifiedFileName() + ".cs", comp);
                }
            });
    }
}