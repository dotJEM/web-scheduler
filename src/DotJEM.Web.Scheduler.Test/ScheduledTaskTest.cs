using DotJEM.AdvParsers;
using DotJEM.Web.Scheduler.Abstractions;
using DotJEM.Web.Scheduler.Triggers;
using NUnit.Framework;

namespace DotJEM.Web.Scheduler.Test;

public class ScheduledTaskTest
{
    [Test]
    public async Task Signal_Once_ExecutesOnce()
    {
        int executed = 0;
        IScheduledTask task = new ScheduledTask("Test Task", TaskExt.ToAsync<bool>(b => executed++), new ThreadPoolProxy(), new PeriodicTrigger(100.Seconds()));

        task.Start();
        await task.Signal();

        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public async Task Signal_ThreeTimes_ExecutesAll()
    {
        int executed = 0;
        IScheduledTask task = new ScheduledTask("Test Task", TaskExt.ToAsync<bool>(b => executed++), new ThreadPoolProxy(), new PeriodicTrigger(100.Seconds()));

        task.Start();
        await task.Signal();
        await task.Signal();
        await task.Signal();

        Assert.That(executed, Is.EqualTo(3));
    }

    [Test]
    public async Task ExecutesOnce()
    {
        int executed = 0;
        IScheduledTask task = new ScheduledTask("Test Task", async b =>
        {
            await Task.Delay(100);
            executed++;
        }, new ThreadPoolProxy(), new PeriodicTrigger(100.Seconds()));

        task.Start();
        await Task.WhenAll(
            task.Signal(true),
            task.Signal(true),
            task.Signal(true),
            task.Signal(true),
            task.Signal(true));

        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public async Task ExecutesAndRetrigger()
    {
        AutoResetEvent waitForStart = new AutoResetEvent(false);
        int executed = 0;
        IScheduledTask task = new ScheduledTask("Test Task", async b =>
        {
            executed++;
            waitForStart.Set();
            await Task.Delay(100);
        }, new ThreadPoolProxy(), new PeriodicTrigger(100.Seconds()));

        task.Start();

        Task first = task.Signal();
        waitForStart.WaitOne();
        Task second = task.Signal();

        await Task.WhenAll(first, second);

        Assert.That(executed, Is.EqualTo(2));
    }
}