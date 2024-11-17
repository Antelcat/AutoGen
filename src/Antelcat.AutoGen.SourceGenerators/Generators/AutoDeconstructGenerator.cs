using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator]
public class AutoDeconstructGenerator : AttributeDetectBaseGenerator<AutoDeconstructAttribute>
{
    protected override bool FilterSyntax(SyntaxNode node) => true;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, _, syntaxArray) = contexts;
        foreach (var syntaxContext in syntaxArray)
        {
            var type    = (syntaxContext.TargetSymbol as INamedTypeSymbol)!;
            var declare = Generate(type);
            if (declare is null) continue;
            var comp = CompilationUnit().AddPartialType(type, t => t.AddMembers(declare));
            context.AddSource(type.GlobalName().ToQualifiedFileName("AutoDeconstruct","Deconstruct"), comp.NormalizeWhitespace().GetText(Encoding.UTF8));
        }
    }

    private static MemberDeclarationSyntax? Generate(ITypeSymbol type)
    {
        var declare = "public void Deconstruct";
        var args    = new List<string>();
        var lines   = new List<string>();
        foreach (var property in type.GetAllMembers()
            .OfType<IPropertySymbol>()
            .Where(x => !x.IsWriteOnly) // readable
            .Where(x => !x.IsImplicitlyDeclared)
            .Where(x => SymbolEqualityComparer.Default.Equals(x.ContainingType, type) // my property 
                        || x.DeclaredAccessibility is not Accessibility.Private))     // not private
        {
            var propType = property.Type.GlobalName();
            var propName = property.MetadataName;
            args.Add(
                $"out {propType}{(property.Type is { IsValueType: false, NullableAnnotation: NullableAnnotation.Annotated } ? "?" : "")} {propName}");
            lines.Add($"{propName} = this.{propName};");
        }

        return ParseMemberDeclaration($"{declare}({string.Join(",", args)}){{ {string.Join("\n",lines)} }}");
    }
}