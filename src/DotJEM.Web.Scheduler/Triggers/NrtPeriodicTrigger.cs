using System;
using System.Diagnostics;

namespace DotJEM.Web.Scheduler.Triggers;

/// <summary>
/// A Trigger used for periodic executions of tasks with a fixed time in between.
/// </summary>
/// <remarks>
/// This trigger is not subject to drift, meaning that if the trigger is set to 10 seconds, execution of the task would happen as close to the
/// 10, 20, 30 etc. marks as possible on a non-realtime system.
///
/// In the event that the actual execution takes longer than the tigger interval.
/// If it's important to execute the task as precisely at the interval as possible on a non-realtime system. Use the <see cref="NrtPeriodicTrigger"/> instead.
/// </remarks>
public class NrtPeriodicTrigger : ITrigger
{
    private readonly TimeSpan timeSpan;
    private long lastRequestedTime = -1;

    public NrtPeriodicTrigger(TimeSpan timeSpan)
    {
        this.timeSpan = timeSpan;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        if (firstExecution)
        {
            timeSpan = this.timeSpan;
            lastRequestedTime = Stopwatch.GetTimestamp();
            return true;
        }
        long lastTicks = lastRequestedTime;
        lastRequestedTime = Stopwatch.GetTimestamp();
        long nextTicks = this.timeSpan.Ticks - GetDateTimeTicksSinceLast(lastTicks);
        timeSpan = nextTicks > 0 ?
            new(nextTicks)
            : TimeSpan.Zero;
        return true;
    }

    private static long GetDateTimeTicksSinceLast(long last)
    {
        long ticks = last - Stopwatch.GetTimestamp();
        if (!Stopwatch.IsHighResolution)
            return ticks;
        
        double dTicks = ticks;
        dTicks *= Stopwatch.Frequency;
        return unchecked((long)dTicks);
    }
}

