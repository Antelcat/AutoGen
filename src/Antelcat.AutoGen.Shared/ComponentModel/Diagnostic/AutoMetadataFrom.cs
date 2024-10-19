using System;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Diagnostic
{
    /// <summary>
    /// Auto generate code using <see cref="Template"/> and members
    /// specified by <see cref="MemberTypes"/> from given <see cref="Type"/>
    /// </summary>
    /// <param name="forType">Type which contains target members</param>
    /// <param name="memberTypes">Types of the members</param>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true,
        Inherited = false)]
    public class AutoMetadataFrom(Type forType, MemberTypes memberTypes) : AutoGenAttribute
    {
        internal Type        ForType     => forType;
        internal MemberTypes MemberTypes => memberTypes;

        /// <summary>
        /// Specify whether compiler generated members are included, default is false
        /// </summary>
        public bool IncludeImplicit { get; set; } = false;

        /// <summary>
        /// Specify <see cref="BindingFlags"/> when searching for members,
        /// default is
        /// <see cref="BindingFlags.NonPublic"/>
        /// <see cref="BindingFlags.Public"/>
        /// <see cref="BindingFlags.Instance"/>
        /// <see cref="BindingFlags.Static"/>
        /// </summary>
        public BindingFlags BindingFlags { get; set; } = BindingFlags.NonPublic |
                                                         BindingFlags.Public    |
                                                         BindingFlags.Instance  |
                                                         BindingFlags.Static;
        
        /// <summary>
        /// Template applying to generate code, you can use
        /// {Name} {PropertyType} {CanRead} ... those members
        /// come from inherits of <see cref="MemberInfo"/>
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// Plain text added to the leading of generated code
        /// </summary>
        public string? Leading { get; set; }
        
        /// <summary>
        /// Plain text added to the final of generated code 
        /// </summary>
        public string? Trailing   { get; set; }
    }
}