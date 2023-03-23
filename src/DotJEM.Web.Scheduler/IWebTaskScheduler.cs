namespace DotJEM.Web.Scheduler;

public interface IWebTaskScheduler
{
    IScheduledTask Schedule(IScheduledTask task);
    void Stop();
}