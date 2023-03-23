using System;
using NCrontab;

namespace DotJEM.Web.Scheduler.Triggers;

public class CronTrigger : ITrigger
{
    private readonly CrontabSchedule cron;

    public CronTrigger(CrontabSchedule cron)
    {
        this.cron = cron;
    }

    public bool TryGetNext(bool firstExecution, out TimeSpan timeSpan)
    {
        timeSpan = cron.GetNextOccurrence(DateTime.Now).Subtract(DateTime.Now);
        return true;
    }
}