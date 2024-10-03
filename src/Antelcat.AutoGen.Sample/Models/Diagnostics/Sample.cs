using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;


[AutoMetadataFrom(typeof(Simulator), MemberTypes.Property,
    Leading = "public global::System.Collections.Generic.IEnumerable<string> Writables(){",
    Template =
        """
        #if {CanWrite}
        yield return nameof({Name});
        #endif

        """,
    Trailing = "}")]
[AutoMetadataFrom(typeof(Simulator), MemberTypes.Property,
    Template =
        """
        public {PropertyType} {Name}Sub { 
            get => this.{Name};
            set {
            #if {CanWrite}
                this.{Name} = value;
            #endif
            } 
        }

        """)]
public partial class Simulator
{
    public string A { get; set; }
    public string B { get; }
    public string C { get; set; }
    public string D { get; }
}