#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Collections.Generic;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;

namespace Antelcat.AutoGen.AssemblyWeavers;

public static class General
{
    public static bool Is(this TypeReference reference, Type type) =>
        reference.IsTypeOf(type.Namespace ?? string.Empty, type.Name);

    public static bool IsRecord(this TypeDefinition type) =>
        type.Properties.Any(p => p.Name == "EqualityContract"
                                 && p.PropertyType.Is(typeof(Type))
                                 && p.IsCompilerGenerated());


    /// <summary>
    /// Whether member contains <see cref="CompilerGeneratedAttribute"/>
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static bool IsCompilerGenerated(this ICustomAttributeProvider provider) =>
        provider.CustomAttributes.Any(a => a.AttributeType.Is(typeof(CompilerGeneratedAttribute)));

    private const BindingFlags MemberBindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider provider)
        where TAttribute : Attribute => provider.GetAttributes<TAttribute>().Any();
    
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this ICustomAttributeProvider provider)
        where TAttribute : Attribute => provider.CustomAttributes.GetAttributes<TAttribute>();

    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Collection<CustomAttribute> attributes)
        where TAttribute : Attribute => attributes.Select(static x => x.As<TAttribute>())
        .Where(static x => x is not null)!;

    public static TAttribute? As<TAttribute>(this CustomAttribute attribute) where TAttribute : Attribute
    {
        var targetType = typeof(TAttribute);
        if (!attribute.AttributeType.Is(targetType)) return null;
        if (attribute.Constructor is not { } attrCtor) return null;
        var attrCtorParams = attrCtor.Parameters;
        var ctor = targetType.GetConstructors(MemberBindingFlags)
            .FirstOrDefault(x =>
            {
                var param = x.GetParameters();
                if (param.Length != attrCtorParams.Count) return false;
                foreach (var (info, index) in param.Select((p, i) => (p, i)))
                {
                    if (!attrCtorParams[index].ParameterType.Is(info.ParameterType))
                    {
                        return false;
                    }
                }

                return true;
            });
        if (ctor is null) return null;
        var ret = ctor.Invoke(attribute.ConstructorArguments.Select(static x => x.Value).ToArray());
        foreach (var namedArgument in attribute.Properties)
        {
            targetType.GetProperty(namedArgument.Name, MemberBindingFlags)?
                .SetValue(ret, namedArgument.Argument.Value);
        }

        foreach (var namedArgument in attribute.Fields)
        {
            targetType.GetField(namedArgument.Name, MemberBindingFlags)?
                .SetValue(ret, namedArgument.Argument.Value);
        }

        return ret as TAttribute;
    }

    public static FieldDefinition? GetBackingField(this PropertyDefinition property) => 
        property.DeclaringType.Fields.FirstOrDefault(x => x.Name == $"<{property.Name}>k__BackingField");

    public static PropertyDefinition? GetFrontingProperty(this FieldDefinition field)
    {
        if (field.Name.Length <= 17) return null;
        var name = field.Name.Substring(1, field.Name.Length - 17);
        return field.DeclaringType.Properties.FirstOrDefault(x => x.Name == name);
    }

    public static bool IsBackingField(this FieldDefinition field) =>
        field.Name.StartsWith("<") && field.Name.EndsWith(">k__BackingField");

    public static bool IsStatic(this PropertyDefinition property) => property.GetMethod.IsStatic || property.SetMethod.IsStatic;
}