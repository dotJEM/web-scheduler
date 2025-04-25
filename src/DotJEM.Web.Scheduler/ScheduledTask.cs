using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.ObservableExtensions.InfoStreams;
using DotJEM.Web.Scheduler.Abstractions;
using DotJEM.Web.Scheduler.Triggers;

namespace DotJEM.Web.Scheduler;

/// <summary>
/// Represents a unit of work that can be scheduled.
/// </summary>
public class ScheduledTask : Disposable, IScheduledTask
{
    public event EventHandler<EventArgs> TaskDisposed;
    
    private static readonly TimeSpan MAX_WAIT = TimeSpan.FromMilliseconds(int.MaxValue);
    

    private readonly object padlock = new();
    private readonly Func<bool,Task> asyncCallback;
    private readonly AutoResetEvent handle = new(false);
    private readonly AutoResetEvent signal = new(false);
    private readonly ActivitySource activitySource;
    private readonly IThreadPool pool;
    private readonly ITrigger trigger;
    private readonly IInfoStream<ScheduledTask> infoStream = new InfoStream<ScheduledTask>();
    private readonly TaskCompletionSource<int> completeCompletionSource;
    private TaskCompletionSource<bool> executionCompletionSource;
    private RegisteredWaitHandle nativeWaitHandle;
    private bool started = false;
    private bool paused = false;
    private bool executing = false;

    /// <inheritdoc />
    public IInfoStream InfoStream => infoStream;

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    private TaskCompletionSource<bool> ExecutionCompletionSource
    {
        get
        {
            lock (padlock)
            {
                return executionCompletionSource;
            }
        }
        set
        {
            lock (padlock)
            {
                executionCompletionSource?.TrySetResult(true);
                executionCompletionSource = value;
            }
        }
    }

    /// <summary>
    /// Creates a new task with a given name, async target and trigger.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="asyncCallback"></param>
    /// <param name="trigger"></param>
    public ScheduledTask(string name, Func<bool, Task> asyncCallback, ITrigger trigger)
        : this(Guid.NewGuid(), name, asyncCallback, new ThreadPoolProxy(), trigger) { }

    /// <summary>
    /// Creates a new task with a given id, name, async target and trigger.
    /// </summary>
    public ScheduledTask(Guid id, string name, Func<bool, Task> asyncCallback, ITrigger trigger)
        : this(id, name, asyncCallback, new ThreadPoolProxy(), trigger) { }

    /// <summary>
    /// Creates a new task with a given name, async target, <see cref="IThreadPool"/> implementation and trigger.
    /// </summary>
    public ScheduledTask(string name, Func<bool, Task> asyncCallback, IThreadPool pool, ITrigger trigger)
        : this(Guid.NewGuid(), name, asyncCallback, pool, trigger) { }

    /// <summary>
    /// Creates a new task with a given id, name, async target, <see cref="IThreadPool"/> implementation and trigger.
    /// </summary>
    public ScheduledTask(Guid id, string name, Func<bool, Task> asyncCallback, IThreadPool pool, ITrigger trigger)
    {
        Id = id;
        Name = name;
        activitySource = ActivitySources.Create<ScheduledTask>();
        completeCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        this.asyncCallback = asyncCallback;
        this.pool = pool;
        this.trigger = trigger;
    }
    
    /// <inheritdoc />
    public void Start()
    {
        CheckDisposed();

        lock (padlock)
        {
            if (started)
                return;
            
            started = true;
            if (trigger.TryGetNext(true, out TimeSpan value))
                RegisterWait(value);

            infoStream.WriteInfo($"Task {Name} was started.");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Task<int> WhenCompleted() => completeCompletionSource.Task;

    /// <summary>
    /// Registers the next call for the scheduled task onto the threadpool.
    /// </summary>
    /// <remarks>
    /// If the task has been disposed this method dows nothing.
    /// </remarks>
    /// <param name="timeout">Time until next execution</param>
    /// <returns>self</returns>
    protected virtual IScheduledTask RegisterWait(TimeSpan timeout)
    {
        if (Disposed)
            return this;

        infoStream.WriteDebug($"Registering next execution for task {Name}.");
        nativeWaitHandle = pool.RegisterWaitForSingleObject(handle, (_, didTimeOut) => ExecuteCallback(didTimeOut).FireAndForget(), null, timeout, true);
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="didTimeOut"></param>
    /// <returns></returns>
    protected virtual async Task ExecuteCallback(bool didTimeOut)
    {
        if (Disposed)
            return;

        lock (padlock)
        {
            if (paused)
                return;

            executing = true;
            ExecutionCompletionSource ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
            signal.Set();
        }

        bool success = true;
        using Activity activity = activitySource.StartActivity(Name);
        try
        {
            infoStream.WriteDebug($"Executing task {Name}.");
            await asyncCallback(!didTimeOut).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            infoStream.WriteError($"Failed to execute task '{Name}'.", ex);
            activity?.SetTag("exception", ex);
            success = false;
        }
        finally
        {
            if (!paused && trigger.TryGetNext(false, out TimeSpan value))
                RegisterWait(value);

            lock (padlock)
            {
                executing = false;
                FinalizeCompletionSource(success);
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        //Ideally we should wait if the task is currently executing, but this is a SyncDispose so.
        //if (executing) !!AWAIT -> executionCompletionSource.Task.Wait(); but this is SYNC vs ASYNC :S.

        nativeWaitHandle?.Unregister(null);
        infoStream.WriteTaskCompleted($"Task '{Name}' was disposed.");
        completeCompletionSource.SetResult(0);
        TaskDisposed?.Invoke(this, EventArgs.Empty);
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public virtual void Pause()
    {
        CheckDisposed();

        paused = true;
        infoStream.WriteDebug($"Task '{Name}' was paused.");
    }

    /// <inheritdoc />
    public virtual void Resume()
    {
        CheckDisposed();
        
        paused = false;
        lock (padlock)
        {
            if (!executing && trigger.TryGetNext(false, out TimeSpan value))
                RegisterWait(value);
        }
        infoStream.WriteDebug($"Task '{Name}' was resumed.");
    }

    /// <inheritdoc />
    public virtual void PauseFor(TimeSpan period)
    {
        CheckDisposed();
        Pause();
        pool.RegisterWaitForSingleObject(new AutoResetEvent(false), (_, _) => Resume(), null, period, true);
        infoStream.WriteDebug($"Task '{Name}' will resume in {period}.");
    }

    /// <inheritdoc />
    public virtual async Task<bool> Signal(bool ignoreIfAlreadyExecution = false)
    {
        CheckDisposed();
        if (!executing)
            return await StartNew();
        
        if (ignoreIfAlreadyExecution) 
            return await ExecutionCompletionSource.Task.ConfigureAwait(false);

        await ExecutionCompletionSource.Task.ConfigureAwait(false);
        return await StartNew();

        Task<bool> StartNew()
        {
            handle.Set();
            signal.WaitOne();
            return ExecutionCompletionSource.Task;
        }
    }

    private void CheckDisposed()
    {
        if (!Disposed)
            return;
        
        ObjectDisposedException ex = new ($"Task '{Name}' was disposed.");
        infoStream.WriteError(ex);
        throw ex;
    }

    private void FinalizeCompletionSource(bool result)
    {
        TaskCompletionSource<bool> currentSource = ExecutionCompletionSource;
        //ExecutionCompletionSource = null;
        currentSource.TrySetResult(result);
    }
}

internal static class TaskExt
{
    public static void FireAndForget(this Task task) { }


    public static Func<T, Task> ToAsync<T>(this Action<T> action)
    {
        #pragma warning disable CS1998
        return async arg => action(arg);
        #pragma warning restore CS1998
    }
}