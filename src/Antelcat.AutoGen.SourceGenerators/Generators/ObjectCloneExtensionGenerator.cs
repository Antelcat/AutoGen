using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.SourceGenerators.Extensions;
using Antelcat.AutoGen.SourceGenerators.Generators.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Accessibility = Antelcat.AutoGen.ComponentModel.Accessibility;

namespace Antelcat.AutoGen.SourceGenerators.Generators;

[Generator]
public class ObjectCloneExtensionGenerator : AttributeDetectBaseGenerator<AutoObjectCloneAttribute>
{
    private const string ClassName = "ObjectExtension";

    protected override bool FilterSyntax(SyntaxNode node) => node is CompilationUnitSyntax or ClassDeclarationSyntax;

    protected override void Initialize(IncrementalGeneratorContexts contexts)
    {
        var (_, context, compilation, syntaxArray) = contexts;

        var cs = (compilation as CSharpCompilation)!;
        var systemValueTypes = cs
            .References
            .Select(x => cs.GetAssemblyOrModuleSymbol(x))
            .OfType<IAssemblySymbol>()
            .SelectMany(x => x.GetAllTypes())
            .Where(x => x is
            {
                DeclaredAccessibility: Microsoft.CodeAnalysis.Accessibility.Public,
                IsValueType          : true,
                IsReadOnly           : true,
                IsGenericType        : false,
                IsRefLikeType        : false
            })
            .Where(x => x.ContainingNamespace.Name == nameof(System))
            .Select(x => x.GetFullyQualifiedName())
            .OrderBy(x => x.Length);

        var filterValueType =
            $"private static bool IsSystemValueType(object obj) => obj is {string.Join("\n        or ", systemValueTypes)};";

        foreach (var classSyntax in syntaxArray.Where(x => x.TargetSymbol is INamedTypeSymbol))
        {
            var typeSymbol = (classSyntax.TargetSymbol as INamedTypeSymbol)!;
            var comp = ParseCompilationUnit(HeaderString + "\n" + CloneHelper(filterValueType) + "\n" +
                                            CompilationUnit()
                                                .AddPartialType(typeSymbol, x =>
                                                    x.AddMembers(Methods.Select(m => ParseMemberDeclaration(m)!)
                                                        .ToArray()), false)
                                                .NormalizeWhitespace()
                                                .GetText(Encoding.UTF8));
            context.AddSource(typeSymbol.GetFullyQualifiedName().ToQualifiedFileName("AutoObjectClone"),
                comp.GetText(Encoding.UTF8));
        }

        foreach (var attrs in syntaxArray.Where(x => x.TargetSymbol is IAssemblySymbol)
            .SelectMany(x => x.GetAttributes<AutoObjectCloneAttribute>())
            .Where(x => x.Namespace.IsValidNamespace())
            .GroupBy(x => x.Namespace))
        {
            var first = attrs.First();
            var comp = ParseCompilationUnit(HeaderString + "\n" + CloneHelper(filterValueType) + "\n" +
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
             {{string.Join("\n", Methods)}}  
          }
          """;

    private const string AccessCloneHelperMemberTypes =
        """
        
        #if NET5_0_OR_GREATER
            [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(CloneHelper.CloneAccessedMemberTypes)]
        #endif
        
        """;

    private const string AccessMemberTypes =
        """
        
        #if NET5_0_OR_GREATER
            [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(CloneAccessedMemberTypes)]
        #endif
        
        """;

    private const string NotNullIfNotNull =
        """
        [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(obj))]
        """;

    private static string RequiresDynamicCode(string message) =>
        $"""
         
         #if NET5_0_OR_GREATER
            [global::System.Diagnostics.CodeAnalysis.RequiresDynamicCode("{message}")]
         #endif
         """;

    private static readonly string[] Methods =
    [
        $"""

         {RequiresDynamicCode("Register() type if it should be cloned at aot")}
         {NotNullIfNotNull}
         public static T? DeepClone<{AccessCloneHelperMemberTypes}T>(this T? obj) => CloneHelper.DeepClone(obj);
         """,
        $"""
         
          {RequiresDynamicCode("Register() type if it should be cloned at aot")}
          {NotNullIfNotNull}
          public static object? DeepClone(this object? obj, {AccessCloneHelperMemberTypes} global::System.Type? type) 
             => CloneHelper.DeepClone(obj, obj?.GetType());
             
         """,
        $"""

         public static void Register<{AccessCloneHelperMemberTypes}T>() => Register(typeof(T));
         """,
        $$"""

          public static void Register({{AccessCloneHelperMemberTypes}} global::System.Type type){ }
          """
    ];


    private static string CloneHelper(string filterValueType) =>
        $$"""
          using System.Collections.Generic;
          using System.Linq;
          
          file static class CloneHelper
          {
          #if NET5_0_OR_GREATER
              public const global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes CloneAccessedMemberTypes =
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors    |
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors |
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields          |
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields       |
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes     |
                  global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
          #endif
                  
              private static readonly global::System.Collections.Concurrent.ConcurrentDictionary<global::System.Type, global::System.Func<object, object>> Mappers = [];
                  
              {{RequiresDynamicCode("Register() type if it should be cloned at aot")}}
              {{NotNullIfNotNull}}
              public static T? DeepClone<
                  {{AccessMemberTypes}}
                  T>(this T? obj) => (T?)DeepClone(obj, obj?.GetType());
          
              {{RequiresDynamicCode("Register() type if it should be cloned at aot")}}
              {{NotNullIfNotNull}}
              public static object? DeepClone(this object? obj, {{AccessMemberTypes}} global::System.Type? type) =>
                  obj is null
                      ? default
                      : type == typeof(object)
                          ? new object()
                          : obj is string or global::System.Delegate or global::System.Enum or global::System.Type || IsSystemValueType(obj)
                              ? obj
                              : GetOrAddMapper(type ?? obj.GetType())(obj);
          
              {{filterValueType}}
          
              {{RequiresDynamicCode("Object creation")}}
              private static global::System.Func<object, object> GetOrAddMapper(
                  {{AccessMemberTypes}}
                  global::System.Type type)
              {
                  if (Mappers.TryGetValue(type, out var mapper)) return mapper;
              
                  if (type.IsArray) return GetArrayMapper(type.GetElementType()!);
              
                  // is {}
                  var fields = GetFields(type).ToArray();
                  global::System.Func<object, object> handle = arg =>
                  {
                      var ret = 
                      #if NET5_0_OR_GREATER
                      global::System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
                      #else
                      global::System.Activator.CreateInstance(type);
                      #endif
                      foreach (var field in fields)
                      {
                          var value = field.GetValue(arg);
                          field.SetValue(ret, value?.DeepClone(value.GetType()));
                      }
              
                      return ret;
                  };
                  Mappers[type] = handle;
                  return handle;
              }
          
              {{RequiresDynamicCode("Array creation")}}
              private static global::System.Func<object, object> GetArrayMapper(global::System.Type elementType) =>
                  obj =>
                  {
                      var arr    = (obj as global::System.Array)!;
                      var length = arr.Length;
                      var array  = global::System.Array.CreateInstance(elementType, length);
                      for (var i = 0; i < length; i++)
                      {
                          var value = arr.GetValue(i);
                          array.SetValue(value?.DeepClone(value.GetType()), i);
                      }
              
                      return array;
                  };
                  
              {{RequiresDynamicCode("Array creation")}}
              private static global::System.Collections.Generic.IEnumerable<global::System.Reflection.FieldInfo> GetFields(global::System.Type type)
                {
                    var fields   = type.GetFields(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
                    var baseType = type.BaseType;
                    return baseType != null ? fields.Concat(GetFields(baseType)) : fields;
                }
          }
          """;
}
