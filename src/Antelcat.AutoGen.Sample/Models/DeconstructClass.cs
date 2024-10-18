using Antelcat.AutoGen.ComponentModel;

namespace Antelcat.AutoGen.Sample.Models;

[AutoDeconstruct]
public partial record DeconstructClass : DeconstructBase
{
   public int D { get; set; }
   
   public int? E { get; set; }
}


public record DeconstructBase
{
    public string  A { get; set; }
    public int?    B { get; set; }
    public string? C { get; set; }
}