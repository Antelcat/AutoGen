using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeaver;

public static class General
{
    public static bool Is(this TypeReference reference, Type type) => type.FullName == reference.TypeFullName();

    public static bool IsRecord(this TypeDefinition type) =>
        type.Properties.Any(p => p.Name == "EqualityContract"
                                 && p.PropertyType.Is(typeof(Type))
                                 && p.CustomAttributes.Any(a =>
                                     a.AttributeType.Is(typeof(CompilerGeneratedAttribute))));
}