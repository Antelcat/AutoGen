namespace Antelcat.AutoGen.AssemblyWeavers;

public interface IWaveArguments
{
    public string  AssemblyFile              { get; set; }
    public string? AssemblyOriginatorKeyFile { get; set; }
    public bool    SignAssembly              { get; set; }
    public bool    DelaySign                 { get; set; }
    public bool    ReadWritePdb              { get; set; }
    public string? IntermediateDirectory     { get; set; }
    public string  References                { get; set; }
}