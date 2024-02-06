using NUnit.Framework;

namespace DotJEM.Web.Scheduler.Test;

public class WebTaskSchedulerTest
{
    [Test]
    public async Task SayHello_ReturnsHello()
    {
        WebTaskScheduler scheduler = new WebTaskScheduler();
        IScheduledTask? task = null;
        bool called = false;
        task = scheduler.ScheduleTask("Foo", timedOut => { called = true; }, TimeSpan.FromMilliseconds(500));
        await Task.WhenAny(task.WhenCompleted(), Task.Delay(TimeSpan.FromSeconds(1)));
        task.Dispose();


        Assert.IsTrue(called);

    }
}