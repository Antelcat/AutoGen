using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Marshal;

namespace Antelcat.AutoGen.Sample.Models.Marshal;

[AutoUnmanagedArray(typeof(char), 16, Accessibility.Private)]
public partial struct UnmanagedArray<T>
{
    
}