using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.Sample.Models.Diagnostics;
using Extra;

[assembly: AutoMetadataFrom(typeof(Simulator), MemberTypes.Property,
    Leading = """
              namespace Extra;
              public class AnotherSimulator{
              """,
    Template = """
               public {PropertyType} {Name} { get; set; }
               """,
    Trailing = """
               }
               """
)]

namespace Some
{
    [AutoMetadataFrom(typeof(Simulator), MemberTypes.Property,
        Leading = "public global::System.Collections.Generic.IEnumerable<string> Writables(){",
        Template =
            """
            yield return nameof(Antelcat.AutoGen.Sample.Models.Diagnostics.Simulator.{Name});

            """,
        Trailing = "}")]
    public partial class Test
    {
        Test()
        {
            new AnotherSimulator().A = "";
        }
    }
}
