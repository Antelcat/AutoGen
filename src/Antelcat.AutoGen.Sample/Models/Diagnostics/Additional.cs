using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.Sample.Models.Diagnostics;


[assembly: AutoMetadataFrom(typeof(Simulator), MemberTypes.Property,
    Leading = """
              public class AnotherSimulator{
              """,
    Template = """
               public {PropertyType} {Name} { get; set; }
               """,
    Trailing = """
               }
               """
)]