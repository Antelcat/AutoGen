using Antelcat.AutoGen.ComponentModel.Diagnostic;
[assembly: AutoRecordPlacebo]

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;


public record A
{
    
}

public record B : A
{

}

public partial record C : B
{

}

partial record C
{
}