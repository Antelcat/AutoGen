using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.Sample.Models.Mapping;

namespace Antelcat.AutoGen.Sample.Extensions.Mapping;

public static partial class MapExtension
{
    [AutoMap(Extra = [nameof(Test)])]
    public static partial Antelcat.AutoGen.Sample.Models.Mapping.SampleEntity ToEntity(this SampleDto dto);

    
    public static void Test()
    {
        new SampleDto("").ToEntity();
    }
}
 