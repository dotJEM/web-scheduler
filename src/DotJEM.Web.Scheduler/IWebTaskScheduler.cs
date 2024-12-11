using DotJEM.ObservableExtensions.InfoStreams;

namespace DotJEM.Web.Scheduler;

public interface IWebTaskScheduler
{
    IInfoStream InfoStream { get; }

    IScheduledTask Schedule(IScheduledTask task);
    void Stop();
}