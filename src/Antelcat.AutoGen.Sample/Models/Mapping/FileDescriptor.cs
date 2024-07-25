using System.ComponentModel.DataAnnotations;
using Antelcat.AutoGen.ComponentModel.Mapping;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.AutoGen.Sample.Models.Mapping;

public partial class FileDescriptor : ObservableObject
{
    public required string FullName { get; set; }
    public virtual  long   Length   { get; set; }

    [ObservableProperty] private string property;
}

public partial class SubFileDescriptor : FileDescriptor
{
    public override long Length { get; set; }

    public int App { get; set; }

    [AutoMap]
    [MapInclude(nameof(Property))]
    [return: MapInclude(nameof(Property))]
    public static partial SubFileDescriptor Apply(FileDescriptor another);

    [AutoMap]
    public  partial object ToAnonymous();

}

public static partial class Extension
{
}