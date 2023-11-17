using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Scheduler.Abstractions;
using DotJEM.Web.Scheduler.Triggers;

namespace DotJEM.Web.Scheduler;

public class ScheduledTask : Disposable, IScheduledTask
{
    public event EventHandler<EventArgs> TaskDisposed;

    private readonly object padlock = new();
    private readonly Func<bool,Task> asyncCallback;
    private readonly AutoResetEvent handle = new(false);
    private readonly ActivitySource activitySource;
    private readonly IThreadPool pool;
    private readonly ITrigger trigger;
    private readonly IInfoStream<ScheduledTask> infoStream = new InfoStream<ScheduledTask>();
    private readonly TaskCompletionSource<int> completeCompletionSource;

    private RegisteredWaitHandle executing;
    private bool started = false;

    public IInfoStream InfoStream => infoStream;

    public Guid Id { get; }
    public string Name { get; }

    public ScheduledTask(string name, Func<bool, Task> asyncCallback, ITrigger trigger)
        : this(Guid.NewGuid(), name, asyncCallback, new ThreadPoolProxy(), trigger) { }

    public ScheduledTask(Guid id, string name, Func<bool, Task> asyncCallback, ITrigger trigger)
        : this(id, name, asyncCallback, new ThreadPoolProxy(), trigger) { }

    public ScheduledTask(string name, Func<bool, Task> asyncCallback, IThreadPool pool, ITrigger trigger)
        : this(Guid.NewGuid(), name, asyncCallback, pool, trigger) { }

    public ScheduledTask(Guid id, string name, Func<bool, Task> asyncCallback, IThreadPool pool, ITrigger trigger)
    {
        Id = id;
        Name = name;
        this.asyncCallback = asyncCallback;
        this.pool = pool;
        this.trigger = trigger;
        activitySource = ActivitySources.Create<ScheduledTask>();
        completeCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public IScheduledTask Start()
    {
        lock (padlock)
        {
            if (started) return this;
            started = true;

            if (trigger.TryGetNext(true, out TimeSpan value))
                RegisterWait(value);
            return this;
        }
    }

    public TaskAwaiter<int> GetAwaiter() => completeCompletionSource.Task.GetAwaiter();

    /// <summary>
    /// Registers the next call for the scheduled task onto the threadpool.
    /// </summary>
    /// <remarks>
    /// If the task has been disposed this method dows nothing.
    /// </remarks>
    /// <param name="timeout">Time untill next execution</param>
    /// <returns>self</returns>
    protected virtual IScheduledTask RegisterWait(TimeSpan timeout)
    {
        if (Disposed)
            return this;

        executing = pool.RegisterWaitForSingleObject(handle, (_, didTimeOut) => ExecuteCallback(didTimeOut).FireAndForget(), null, timeout, true);
        return this;
    }

    protected async Task<bool> ExecuteCallback(bool didTimeOut)
    {
        if (Disposed)
            return false;

        using Activity activity = activitySource.StartActivity(Name);
        try
        {
            await asyncCallback(!didTimeOut).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            infoStream.WriteError($"Failed to execute task '{Name}'.", ex);
            activity?.SetTag("exception", ex);
            return false;
        }
        finally
        {
            if (trigger.TryGetNext(false, out TimeSpan value))
                RegisterWait(value);
        }
    }

    /// <summary>
    /// Marks the task for shutdown and signals any waiting tasks.
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        base.Dispose(disposing);
        executing?.Unregister(null);
        Signal();

        infoStream.WriteTaskCompleted($"Task '{Name}' completed.");

        TaskDisposed?.Invoke(this, EventArgs.Empty);
        completeCompletionSource.SetResult(0);
    }

    public virtual IScheduledTask Signal()
    {
        handle.Set();
        return this;
    }

    public IScheduledTask Signal(TimeSpan delay)
    {
        pool.RegisterWaitForSingleObject(new AutoResetEvent(false), (_, _) => Signal(), null, delay, true);
        return this;
    }
}

internal static class TaskExt
{
    public static void FireAndForget(this Task task)
    {

    }
    public static Func<T, Task> ToAsync<T>(this Action<T> action)
    {
#pragma warning disable CS1998
        return async arg => action(arg);
#pragma warning restore CS1998
    }
}