using System;
using System.Collections.Concurrent;
using DotJEM.ObservableExtensions.InfoStreams;

namespace DotJEM.Web.Scheduler;

public class WebTaskScheduler : IWebTaskScheduler
{
    private readonly ConcurrentDictionary<Guid, IScheduledTask> tasks = new();
    private readonly IInfoStream<WebTaskScheduler> infoStream = new InfoStream<WebTaskScheduler>();

    public IInfoStream InfoStream => infoStream;

    public IScheduledTask Schedule(IScheduledTask task)
    {
        if (!tasks.TryAdd(task.Id, task))
            throw new ArgumentException($"There is already a task with ID '{task.Id}' added to the scheduler.");

        IDisposable subscription = task.InfoStream.Subscribe(infoStream);
        task.TaskDisposed += (_, _) =>
        {
            tasks.TryRemove(task.Id, out task);
            subscription.Dispose();
        };

        task.Start();
        return task;
    }

    public void Stop()
    {
        foreach (IScheduledTask task in tasks.Values)
            task.Dispose();
    }
}