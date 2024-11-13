using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Internal;

public class TestGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider((x, t) => true, (x, t) => x);
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()), (a, b) =>
        {
            var csComp  = (b.Left as CSharpCompilation)!;
            var collector = csComp
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
                    var type = x.Parameters[1].Type;
                    var returnName = type.GetFullyQualifiedName();
                    var declare =
                        $"public static {returnName} To{type.MetadataName}{GenericDeclaration(x)}(this string? str) {GenericConstrain(x)} => {type.ContainingType.GetFullyQualifiedName()}.TryParse(str, out var value) : value : default;";
                    return ParseMemberDeclaration(declare);
                })
                .ToList();
           
        });
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