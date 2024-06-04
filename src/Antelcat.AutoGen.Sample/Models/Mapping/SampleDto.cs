namespace Antelcat.AutoGen.Sample.Models.Mapping;

public class SampleDto(string other) 
{
    public int     Id    { get; set; }
    public string? Name  { get; set; }
    public string? Email { get; set; }
    public long    DateTime  { get; set; }
}