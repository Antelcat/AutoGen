using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample.Models.Mapping;

public partial class FileDescriptor
{
    public required string FullName { get; set; }
    public          long   Length   { get; set; }

    [AutoMap]
    public partial void Apply(FileDescriptor another);
}

public static partial class Extension
{
}