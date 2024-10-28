using Antelcat.AutoGen.ComponentModel.Threading;

namespace Antelcat.AutoGen.Sample.Models.Threading;

[AutoParallelTaskScheduler]
public partial class LimitedThreadScheduler
{
    public override int MaximumConcurrencyLevel => 3;
}