using System;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class AutoExtractInterfaceAttribute(Accessibility accessibility = Accessibility.Public) : AutoGenAttribute
{
    internal Accessibility Accessibility => accessibility;

    /// <summary>
    /// Name can be formatted by {Name}, Default is I{Name}
    /// </summary>
    public string? NamingTemplate { get; set; } = "I{Name}";
    
    /// <summary>
    /// Generated namespace, default same namespace with source type
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Defines interfaces which generated interface inherits 
    /// </summary>
    public Type[]? Interfaces { get; set; }

    /// <summary>
    /// Defines indexes of generic parameters received from this type
    /// <p/>
    /// consider :
    /// <code>
    /// <![CDATA[
    /// [AutoInterfaceFromAttribute(ReceiveGeneric = [2,0])]
    /// class SourceCodeClass<TA,TB,TC> where TC : new()
    /// ]]>
    /// </code>
    /// will generate
    /// <code>
    /// <![CDATA[
    /// interface IGeneratedInterface<TC,TA> where TC : new()
    /// ]]>
    /// </code>
    /// generated interface receive TC, TA by order
    /// </summary>
    public int[]? ReceiveGeneric { get; set; }

    /// <summary>
    /// Defines indexes of generic parameters passed to inherit interfaces
    /// <p/>
    /// consider :
    /// <code>
    /// <![CDATA[
    /// [AutoInterfaceFromAttribute(ReceiveGeneric = [2,0], PassGeneric = ["1", "1,0"], Interfaces = [typeof(IList<>), typeof(IDictionary<,>)])]
    /// class SourceCodeClass<TA,TB,TC> where TC : new()
    /// ]]>
    /// </code>
    /// will generate
    /// <code>
    /// <![CDATA[
    /// interface IGeneratedInterface<TC,TA> : IList<TA>, IDictionary<TA,TC> where TC : new()
    /// ]]>
    /// </code>
    /// generated interface receive TC, TA by order and pass TA to the first inherit type IList
    /// </summary>
    public string[]? PassGeneric { get; set; }
    
    /// <summary>
    /// Defines ths generic constrains, default is <see cref="GenericConstrain.Inherit"/> for all
    /// </summary>
    public GenericConstrain[]? GenericConstrains { get; set; }

    /// <summary>
    /// Specify kinds of members to be extracted, default is
    /// <see cref="MemberTypes.Property"/> | <see cref="MemberTypes.Method"/> | <see cref="MemberTypes.Event"/>
    /// </summary>
    public MemberTypes MemberTypes { get; set; } = MemberTypes.Property | MemberTypes.Method | MemberTypes.Event;

    /// <summary>
    /// Excluded members with specified name
    /// </summary>
    public string[]? Exclude { get; set; }

    [Flags]
    public enum GenericConstrain
    {
        /// <summary>
        /// inherits from source type
        /// </summary>
        Inherit = 0x1,

        /// <summary>
        /// new()
        /// </summary>
        New = 0x2,

        /// <summary>
        /// in
        /// </summary>
        In = 0x4,

        /// <summary>
        /// out
        /// </summary>
        Out = 0x8,

        /// <summary>
        /// class
        /// </summary>
        Class = 0x10,

        /// <summary>
        /// struct
        /// </summary>
        Struct = 0x20,

        /// <summary>
        /// unmanaged
        /// </summary>
        Unmanaged = 0x40,

        /// <summary>
        /// base types from source type
        /// </summary>
        Types = 0x80
    }
}