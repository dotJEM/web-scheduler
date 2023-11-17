using System;
using System.Threading.Tasks;
using DotJEM.Web.Scheduler.Triggers;
using NCrontab;

namespace DotJEM.Web.Scheduler;

public static class TaskSchedulerExtensions
{
    public static IScheduledTask Schedule(this IWebTaskScheduler self, string name, Action<bool> callback, string expression)
        => self.Schedule(new ScheduledTask(name, callback.ToAsync(), Trigger.Parse(expression)));

    public static IScheduledTask ScheduleTask(this IWebTaskScheduler self, string name, Action<bool> callback, TimeSpan interval)
        => self.Schedule(new ScheduledTask(name, callback.ToAsync(), new PeriodicTrigger(interval)));

    public static IScheduledTask ScheduleCallback(this IWebTaskScheduler self, string name, Action<bool> callback, TimeSpan? timeout)
        => self.Schedule(new ScheduledTask(name, callback.ToAsync(), new SingleFireTrigger(timeout ?? TimeSpan.Zero)));

    public static IScheduledTask ScheduleCron(this IWebTaskScheduler self, string name, Action<bool> callback, string trigger)
        => self.Schedule(new ScheduledTask(name, callback.ToAsync(), new CronTrigger(CrontabSchedule.Parse(trigger))));

    public static IScheduledTask Schedule(this IWebTaskScheduler self, string name, Func<bool,Task> asyncCallback, string expression)
        => self.Schedule(new ScheduledTask(name, asyncCallback, Trigger.Parse(expression)));

    public static IScheduledTask ScheduleTask(this IWebTaskScheduler self, string name, Func<bool,Task> asyncCallback, TimeSpan interval)
        => self.Schedule(new ScheduledTask(name, asyncCallback, new PeriodicTrigger(interval)));

    public static IScheduledTask ScheduleCallback(this IWebTaskScheduler self, string name, Func<bool,Task> asyncCallback, TimeSpan? timeout)
        => self.Schedule(new ScheduledTask(name, asyncCallback, new SingleFireTrigger(timeout ?? TimeSpan.Zero)));

    public static IScheduledTask ScheduleCron(this IWebTaskScheduler self, string name, Func<bool,Task> asyncCallback, string trigger)
        => self.Schedule(new ScheduledTask(name, asyncCallback, new CronTrigger(CrontabSchedule.Parse(trigger))));
}