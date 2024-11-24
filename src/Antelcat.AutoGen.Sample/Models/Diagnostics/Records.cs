using Antelcat.AutoGen.ComponentModel.Diagnostic;
[assembly: AutoRecordPlacebo]

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;


public record A
{
    
}

public record B
{
    [RecordIgnore]
    public C C { get; set; }
}

public partial record C : B
{
    
}

partial record C
{
    public B B { get; set; }
}