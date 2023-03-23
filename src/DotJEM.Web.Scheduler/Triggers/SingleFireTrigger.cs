using System;

namespace DotJEM.Web.Scheduler.Triggers;

public class SingleFireTrigger : ITrigger
{
    private readonly TimeSpan timeSpan;

    public SingleFireTrigger(TimeSpan timeSpan)
    {
        this.timeSpan = timeSpan;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = this.timeSpan;
        return firstExecution;
    }
}