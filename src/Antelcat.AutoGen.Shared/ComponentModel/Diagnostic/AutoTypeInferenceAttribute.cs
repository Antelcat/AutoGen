using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

/// <summary>
/// Auto generate named type from temporary declared type
/// <p>considering:</p>
/// <code>
/// var tempObject = new TempClass          //error type that not been declared yet
/// {
///     IntProp = 1,                        //any name of the property
///     StringProp = someObject.ToString(), //called from somewhere else
/// }
/// </code>
/// which will generate
/// <code>
/// public partial class TempClass //optional declaration
/// {
///     int    IntProp    { get; set; }
///     string StringProp { get; set; }
/// }
/// </code>
/// if the same full qualified name of a type be detected above 1 time, and
/// they have different type using same name, it will inference the most
/// compatible property type
/// </summary>
/// <param name="accessibility"></param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class AutoTypeInferenceAttribute(
    Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal readonly Accessibility Accessibility = accessibility;

    /// <summary>
    /// If set, type only be created when type name starts within specified prefixes
    /// </summary>
    public string[]? Prefixes { get; set; }
    
    /// <summary>
    /// If set, type only be created when type name ends within specified suffixes
    /// </summary>
    public string[]? Suffixes { get; set; }

    /// <summary>
    /// Define what kind of type to be generated, default is <see cref="TypeKind.Class"/>
    /// </summary>
    public TypeKind Kind { get; set; } = TypeKind.Class;
    
    /// <summary>
    /// Extra attribute metadata on each generated type, such as
    /// <code>
    /// [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    /// public partial class GeneratedClass
    /// </code>
    /// </summary>
    public string? AttributeOnType { get; set; }
    
    /// <summary>
    /// Extra attribute metadata on each generated property
    /// </summary>
    public string? AttributeOnProperty { get; set; }

    public enum TypeKind
    {
        Class,
        Record
    }
}