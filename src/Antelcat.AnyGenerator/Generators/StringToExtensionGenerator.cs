using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.AnyGenerator.Generators;

[Generator]
public class StringToExtensionGenerator : IIncrementalGenerator
{
    private const string ClassName = "GenerateStringToAttribute";

    private const string Attribute =
        $$"""
          namespace {{Namespace}};

          [global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false)]
          public class {{ClassName}} : global::System.Attribute
          {
              public {{ClassName}}(string? Namespace = nameof(System)) { }
          }

          """;
    private static string GetGenericConversion(Type? type)
    {
        if (type is null || type.GenericParameterAttributes == GenericParameterAttributes.None) return string.Empty;
        var sb = new StringBuilder($" where {type.Name} :");
        
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            sb.Append(" class,");
        }
        if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            sb.Append(" struct,");
        }
        else if (type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            sb.Append(" new(),");
        }

        return sb.Remove(sb.Length - 1, 1).ToString();
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource($"{Namespace}.{ClassName}.g.cs", SourceText(Attribute));
        });
        
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{Namespace}.{ClassName}",
            (ctx, t) => ctx is CompilationUnitSyntax,
            (ctx, t) => (ctx.TargetNode as CompilationUnitSyntax)!);
        context.RegisterSourceOutput(context
                .CompilationProvider
                .Combine(provider.Collect()),
            (ctx, t) =>
            {
                var names = t.Right
                    .SelectMany(x => x.AttributeLists
                        .SelectMany(y => y.Attributes))
                    .Select(x => x.ArgumentList?
                                     .Arguments
                                     .FirstOrDefault()?
                                     .GetArgumentString()
                                 ?? nameof(System))
                    .Where(x => x.IsInvalidNamespace())
                    .Distinct();
                foreach (var name in names)
                {
                    var contexts  = new List<(Type Type, MethodInfo method, Type? GType)>();
                    foreach (var type in typeof(string).Assembly.ExportedTypes)
                    {
                        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(x => x.Name == "TryParse" && x.GetParameters().Length == 2);
                        if (method is null) continue;
                        contexts.Add(method.IsGenericMethod
                            ? (type, method, method.GetGenericArguments().First())
                            : (type, method, null));
                    }

                    var content = string.Join("\n", contexts
                        .Select(x =>
                            $"    public static {x.GType?.Name ?? Global(x.Type)}{Nullable(x.GType ?? x.Type)} To{x.Type.Name}{Generic(x.GType?.Name)}(this string? str){
                                GetGenericConversion(x.GType)} => {Global(x.Type)}.TryParse{Generic(x.GType?.Name)}(str, out var result) ? result : default;\n"));

                    /*var converters = string.Join("\n", contexts
                        .Where(x => x.GType == null)
                        .Select(x => $$"""
                                       public class String{{x.Type.Name}}Converter : {{Global(typeof(TypeConverter))}}
                                       {
                                           public override {{Global(typeof(object))}} ConvertTo(
                                               {{Global(typeof(ITypeDescriptorContext))}}? _,
                                               {{Global(typeof(CultureInfo))}}? __,
                                               {{Global(typeof(object))}}? value,
                                               {{Global(typeof(Type))}} ___) => value?.ToString();
                                           
                                           public override {{Global(typeof(object))}}? ConvertFrom(
                                               {{Global(typeof(ITypeDescriptorContext))}}? _,
                                               {{Global(typeof(CultureInfo))}}? __,
                                               {{Global(typeof(object))}}? value) => (value as string).To{{x.Type.Name}}();
                                           
                                           public override {{Global(typeof(bool))}} CanConvertTo(
                                               {{Global(typeof(ITypeDescriptorContext))}}? _,
                                               {{Global(typeof(Type))}}? destinationType) => destinationType == typeof({{Global(x.Type)}});
                                       
                                          	public override {{Global(typeof(bool))}} CanConvertFrom(
                                               {{Global(typeof(ITypeDescriptorContext))}}? _,
                                               {{Global(typeof(Type))}} sourceType) => sourceType == typeof({{Global(typeof(string))}});
                                       }

                                       """));*/

                    ctx.AddSource($"{name}.StringToExtension.g.cs", SourceText($$"""
                                                                       #nullable enable
                                                                       using System;

                                                                       namespace {{name}}
                                                                       {
                                                                           public static partial class StringExtension
                                                                           {
                                                                           {{content.Replace("\n", "\n\t")}}
                                                                           }
                                                                       }
                                                                       """));

                    /*ctx.AddSource("StringValueConverters.g.cs", SourceText.From($$"""
                                                                                  #nullable enable
                                                                                  using System;

                                                                                  namespace System.ComponentModel
                                                                                  {
                                                                                      {{converters.Replace("\n","\n\t")}}
                                                                                  }
                                                                                  """, Encoding.UTF8));*/
                }

            });

    }
}
