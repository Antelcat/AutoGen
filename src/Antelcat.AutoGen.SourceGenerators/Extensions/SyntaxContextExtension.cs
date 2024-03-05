using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Extensions;

public static class SyntaxContextExtension
{
    public static IEnumerable<T> GetAttributes<T>(this GeneratorAttributeSyntaxContext context) where T : Attribute =>
        context.Attributes.GetAttributes<T>();

    public static IEnumerable<T> GetAttributes<T>(this IEnumerable<AttributeData> attributes) where T : Attribute =>
        attributes.Where(static attributeData =>
                attributeData.AttributeClass?.HasFullyQualifiedMetadataName(typeof(T).FullName) is true)
            .Select(static attributeData => attributeData.ToAttribute<T>());
    
    
}