﻿using System;
using DotJEM.Web.Scheduler.Triggers;
using NCrontab;

namespace DotJEM.Web.Scheduler;

public static class TaskSchedulerExtensions
{
    public static IScheduledTask Schedule(this IWebTaskScheduler self, string name, Action<bool> callback, string expression)
        => self.Schedule(new ScheduledTask(name, callback, Trigger.Parse(expression)));

    public static IScheduledTask ScheduleTask(this IWebTaskScheduler self, string name, Action<bool> callback, TimeSpan interval)
        => self.Schedule(new ScheduledTask(name, callback, new PeriodicTrigger(interval)));

    public static IScheduledTask ScheduleCallback(this IWebTaskScheduler self, string name, Action<bool> callback, TimeSpan? timeout)
        => self.Schedule(new ScheduledTask(name, callback, new SingleFireTrigger(timeout ?? TimeSpan.Zero)));

    public static IScheduledTask ScheduleCron(this IWebTaskScheduler self, string name, Action<bool> callback, string trigger)
        => self.Schedule(new ScheduledTask(name, callback, new CronTrigger(CrontabSchedule.Parse(trigger))));
}