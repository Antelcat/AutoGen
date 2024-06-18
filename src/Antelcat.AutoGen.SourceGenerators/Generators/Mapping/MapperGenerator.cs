using static Microsoft.CodeAnalysis.Diagnostic;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.SourceGenerators.Generators.Mapping.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Mapping;

[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
    internal static readonly string AutoMap    = typeof(AutoMapAttribute).FullName!;
    internal static readonly string MapExclude = typeof(MapExcludeAttribute).FullName!;
    internal static readonly string MapInclude = typeof(MapIncludeAttribute).FullName!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoMap,
            static (ctx, t) => ctx is MethodDeclarationSyntax,
            static (ctx, _) => ctx);
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            static (ctx, tuple) =>
            {
                foreach (var group in tuple.Right
                             .GroupBy(static x =>
                                 (x.TargetSymbol as IMethodSymbol)!.ContainingType, SymbolEqualityComparer.Default))
                {
                    var @class  = (group.Key as INamedTypeSymbol)!;
                    var partial = @class.PartialTypeDeclaration();
                     foreach (var syntax in group)
                    {
                        var method   = (syntax.TargetSymbol as IMethodSymbol)!;
                        var location = syntax.TargetNode.GetLocation();
                        if (@class.TypeKind == TypeKind.Interface)
                        {
                            ctx.ReportDiagnostic(
                                Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                    location,
                                    nameof(AutoMapAttribute),
                                    "not be declared in an interface"
                                ));
                            continue;
                        }

                        ITypeSymbol fromType;
                        ITypeSymbol toType;
                        var         mode = FilterMethodAndReport(method, ctx, location);
                        switch (mode)
                        {
                            case MethodMode.ArgumentError:
                                ctx.ReportDiagnostic(
                                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                        location,
                                        nameof(AutoMapAttribute),
                                        "zero or one parameter"
                                    ));
                                continue;
                            case MethodMode.MapSelf:
                                if (@class.IsStatic)
                                {
                                    ctx.ReportDiagnostic(
                                        Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                                            location,
                                            nameof(AutoMapAttribute),
                                            "not in a static class"
                                        ));
                                    continue;
                                }

                                fromType = method.ReturnsVoid ? method.Parameters[0].Type : @class;
                                toType   = method.ReturnsVoid ? @class : method.ReturnType;
                                break;
                            case MethodMode.MapBetween:
                                fromType = method.Parameters[0].Type;
                                toType   = method.ReturnType;
                                break;
                            case MethodMode.Invalid:
                            default: continue;
                        }

                        var methodSyntax = (syntax.TargetNode as MethodDeclarationSyntax)!;
                        partial = partial.AddMembers(
                            methodSyntax.FullQualifiedPartialMethod(method)
                                .AddGenerateAttribute(typeof(MapperGenerator))
                                .WithBody(new MapInfo(method, fromType, toType).Map()));
                    }

                    ctx.AddSource($"{nameof(AutoMap)}__.{@class
                        .GetFullyQualifiedName()
                        .ToQualifiedFileName()}.g.cs",
                        CompilationUnit()
                            .AddPartialType(@class, x => partial)
                            .NormalizeWhitespace()
                            .GetText(Encoding.UTF8));
                }
            });
    }

    private static MethodMode FilterMethodAndReport(IMethodSymbol method,
        SourceProductionContext context,
        Location location)
    {
        if (!method.IsPartialDefinition)
        {
            context.ReportDiagnostic(
                Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                    location,
                    nameof(AutoMapAttribute),
                    "should be partial definition"
                ));
        }
        
        if (method.ReturnsVoid)
            return method.Parameters.Length == 1 ? MethodMode.MapSelf : MethodMode.ArgumentError;

        switch (method.ReturnType.TypeKind)
        {
            case TypeKind.Interface or TypeKind.Dynamic:
                context.ReportDiagnostic(
                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                        location,
                        nameof(AutoMapAttribute),
                        "return actual type"
                    ));
                return MethodMode.Invalid;
            case TypeKind.Class when method.IsAbstract:
                context.ReportDiagnostic(
                    Create(Diagnostics.Error.AM0002(nameof(MapperGenerator)),
                        location,
                        nameof(AutoMapAttribute),
                        "return none abstract type"
                    ));
                return MethodMode.Invalid;
        }
     
        return method.Parameters.Length switch
        {
            0 => MethodMode.MapSelf,
            1 => MethodMode.MapBetween,
            _ => MethodMode.ArgumentError
        };
    }

    private enum MethodMode
    {
        Invalid,
        MapSelf,
        MapBetween,
        ArgumentError
    }
    
}