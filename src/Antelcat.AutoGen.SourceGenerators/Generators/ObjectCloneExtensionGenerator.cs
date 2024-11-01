using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

public class ObjectCloneExtensionGenerator : AttributeDetectBaseGenerator<AutoObjectCloneAttribute>
{
    private const string ClassName = "ObjectExtension";

    protected override bool FilterSyntax(SyntaxNode node) => node is CompilationUnitSyntax or ClassDeclarationSyntax;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, _, syntaxArray) = contexts;
        foreach (var classSyntax in syntaxArray.Where(x => x.TargetSymbol is INamedTypeSymbol))
        {
            var typeSymbol = (classSyntax.TargetSymbol as INamedTypeSymbol)!;
            var comp = ParseCompilationUnit(HeaderString + "\n" + Usings + CloneHelper + "\n" +
                                            CompilationUnit()
                                                .AddPartialType(typeSymbol, x =>
                                                    x.AddMembers(ParseMemberDeclaration(Methods)!), false)
                                                .NormalizeWhitespace()
                                                .GetText(Encoding.UTF8));
            context.AddSource($"AutoObjectClone__{typeSymbol.GetFullyQualifiedName().ToQualifiedFileName()}.g.cs",
                comp.GetText(Encoding.UTF8));
        }

        foreach (var attrs in syntaxArray.Where(x => x.TargetSymbol is IAssemblySymbol)
            .SelectMany(x => x.GetAttributes<AutoObjectCloneAttribute>())
            .Where(x => x.Namespace.IsValidNamespace())
            .GroupBy(x => x.Namespace))
        {
            var first = attrs.First();
            var comp = ParseCompilationUnit(HeaderString + "\n" + Usings + CloneHelper + "\n" +
                                            CompilationUnit()
                                                .AddMembers(NamespaceDeclaration(ParseName(first.Namespace))
                                                    .AddMembers(
                                                        ParseMemberDeclaration(ObjectExtension(first.Accessibility))!))
                                                .NormalizeWhitespace().GetText(Encoding.UTF8));
            context.AddSource($"AutoObjectClone__{first.Namespace}.{ClassName}.g.cs",
                comp.NormalizeWhitespace().GetText(Encoding.UTF8));
        }
    }


    private static string ObjectExtension(Accessibility accessibility) =>
        accessibility.ToCodeString() + ' ' +
        $$"""
        
        static partial class ObjectExtension{
           {{Methods}}  
        }
        """;

    private const string Methods =
        """
        
        [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(obj))]
        public static T? MemoryClone<
        [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | global::System
                .Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>(this T? obj) => CloneHelper.MemoryClone(obj);
        """;
    
    private const string Usings =
        """
        
        using global::System.Diagnostics.CodeAnalysis;
        using global::System.Reflection;
        using global::System.Runtime.CompilerServices;
        """;

    private const string CloneHelper =
        """
        
        #if NET5_0_OR_GREATER
        file static class CloneHelper
        {
            private sealed class RawData
            {
                public byte Data;
            }
        
            private delegate ref byte GetRawDataHandler(object obj);
        
            private static ref byte GetRawDataRef(object obj) => ref global::System.Runtime.CompilerServices.Unsafe
                .As<global::CloneHelper.RawData>(obj).Data;
        
            private static global::CloneHelper.GetRawDataHandler GetRawData;
            private static global::System.Func<object, nuint>    GetRawObjectDataSize;
        
            static CloneHelper()
            {
                global::CloneHelper.GetRawData = typeof(global::System.Runtime.CompilerServices.RuntimeHelpers).GetMethod(
                        nameof(global::CloneHelper.GetRawData),
                        global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!
                    .CreateDelegate<global::CloneHelper.GetRawDataHandler>();
                global::CloneHelper.GetRawObjectDataSize =
                    typeof(global::System.Runtime.CompilerServices.RuntimeHelpers).GetMethod(
                            nameof(global::CloneHelper.GetRawObjectDataSize),
                            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!
                        .CreateDelegate<global::System.Func<object, nuint>>();
            }
        
            [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(obj))]
            public static T? MemoryClone<
                [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                    global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | global::System
                        .Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors)]
                T>(T? obj)
            {
                if (obj is null) return default;
                var newObj = (T)global::System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));
                if (newObj is null) return default!;
                var size = global::CloneHelper.GetRawObjectDataSize(obj);
                unsafe
                {
                    global::System.Buffer.MemoryCopy(
                        global::System.Runtime.CompilerServices.Unsafe.AsPointer(
                            ref global::CloneHelper.GetRawDataRef(obj)),
                        global::System.Runtime.CompilerServices.Unsafe.AsPointer(
                            ref global::CloneHelper.GetRawDataRef(newObj)),
                        size, size);
                }
        
                return newObj;
            }
        }
        #endif
        """;
}