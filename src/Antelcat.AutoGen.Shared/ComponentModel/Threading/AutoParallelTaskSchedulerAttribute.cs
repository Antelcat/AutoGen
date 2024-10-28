using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Threading
{
    /// <summary>
    /// Auto generate implements of parallel task scheduler and inherit <see cref="System.Threading.Tasks.TaskScheduler"/>
    /// </summary>
    /// <param name="parallelNumber">if less than 0, should manually set <see cref="System.Threading.Tasks.TaskScheduler.MaximumConcurrencyLevel"/></param>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoParallelTaskSchedulerAttribute(int parallelNumber = 0) : AutoGenAttribute
    {
        internal int ParallelNumber => parallelNumber;
    }
}