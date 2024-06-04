namespace Antelcat.AutoGen.Sample.Models.Mapping;

public class FileDescriptor
{
    public required string FullName { get; set; }
    public          long   Length   { get; init; }
}

public static partial class Extension
{
}