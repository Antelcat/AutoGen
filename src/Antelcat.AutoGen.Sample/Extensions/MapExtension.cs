
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.Sample.Models;

namespace Antelcat.AutoGen.Sample.Extensions;

public static partial class MapExtension
{
    [AutoMap]
    public static partial SampleEntity ToEntity(this SampleDto dto);

    
    public static void Test()
    {
        new SampleDto("").ToEntity();
    }
}
