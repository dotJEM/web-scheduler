using System;
using System.Threading;

namespace DotJEM.Web.Scheduler.Triggers;

/// <summary>
/// A Trigger used for periodic executions of tasks with a fixed time in between.
/// </summary>
/// <remarks>
/// This trigger is subject to drift, meaning that the next execution of a task would be "ExecutionTime + TriggerTime".
/// If it's important to execute the task as precisely at the interval as possible on a non-realtime system. Use the <see cref="NrtPeriodicTrigger"/> instead.
/// </remarks>
public class PeriodicTrigger : ITrigger
{
    private readonly TimeSpan timeSpan;

    public PeriodicTrigger(TimeSpan timeSpan)
    {
        this.timeSpan = timeSpan;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = this.timeSpan;
        return true;
    }
}