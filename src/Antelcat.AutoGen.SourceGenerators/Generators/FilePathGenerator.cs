using System.Diagnostics;
using System.Linq;
using Antelcat.AutoGen.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
public class FilePathGenerator : IIncrementalGenerator
{
    private const string Class = "FilePath";
    
    private static string AutoFilePathAttribute = typeof(AutoFilePathAttribute).FullName!;
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoFilePathAttribute,
            (ctx, t) => ctx is CompilationUnitSyntax,
            (ctx, t) => ctx
        );
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, tuple) =>
            {
                foreach (var syntaxContext in tuple.Right)
                {
                    var attr = syntaxContext.Attributes
                        .Where(static x => x.AttributeClass?.GetFullyQualifiedName() == global + AutoFilePathAttribute)
                        .Select(static x => x.ToAttribute<AutoFilePathAttribute>())
                        .FirstOrDefault(static x => x.Namespace.IsValidNamespace());
                    if (attr is null) continue;
                    var text = Antelcat.AutoGen.FilePath.Text.Replace(
                        $"{nameof(Antelcat)}.{nameof(AutoGen)}.{nameof(SourceGenerators)}", attr.Namespace);
                    if (attr.Accessibility is Accessibility.Internal)
                    {
                        text = text.Replace("public readonly", "internal readonly");
                    }

                    ctx.AddSource($"{attr.Namespace}.{Class}.cs", SourceText(text));
                }
            });
    }
}