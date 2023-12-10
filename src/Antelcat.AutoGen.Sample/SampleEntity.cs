
using Antelcat.AutoGen.ComponentModel;

namespace Antelcat.AutoGen.Sample;

// This code will not compile until you build the project with the Source Generators

public partial class SampleEntity
{
    public int     Id   { get; } = 42;
    public string? Name { get; } = "Sample";
}


[GenerateStringTo]
public static partial class Extensions
{
    
}