using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample.Models.Mapping;

public partial class FileDescriptor
{
    public required string FullName { get; set; }
    public    virtual      long   Length   { get; set; }

   
}

public partial class SubFileDescriptor : FileDescriptor
{
    public override long Length { get; set; }
    
    public int App { get; set; }
    
    [AutoMap]
    public static partial SubFileDescriptor Apply(FileDescriptor another);
}

public static partial class Extension
{
}