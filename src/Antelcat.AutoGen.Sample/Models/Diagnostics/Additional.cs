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


file class Test
{
    Test()
    {
        new AnotherSimulator().A = "";
    }
}