using System;

namespace DotJEM.Web.Scheduler.Triggers;

public interface ITrigger
{
    bool TryGetNext(bool firstExecution, out TimeSpan timeSpan);
}