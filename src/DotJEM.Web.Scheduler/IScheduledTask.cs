using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DotJEM.ObservableExtensions.InfoStreams;

namespace DotJEM.Web.Scheduler;

/// <summary>
/// 
/// </summary>
public interface IScheduledTask : IDisposable
{
    /// <summary>
    /// Raised after Dispose is called on the task and it has run to completion.
    /// </summary>
    event EventHandler<EventArgs> TaskDisposed;

    /// <summary>
    /// A Unique ID of the task.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// A friendly name of the task, e.g. to be used for logs etc.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// An info stream reporting events from the task.
    /// </summary>
    public IInfoStream InfoStream { get; }

    /// <summary>
    /// Starts the task
    /// </summary>
    void Start();

    /// <summary>
    /// Signals that the task should run immediately regardless of its trigger.
    /// </summary>
    /// <param name="ignoreIfAlreadyExecution">
    /// Flag that indicates if no new executions should be planned if the task is already executing.
    /// <br/> This is false by default as it's not possible to see exactly where in it's execution a task is,
    /// so it could be finalizing.
    /// </param>
    /// <returns>A Task that is signaled when the next execution is completed.</returns>
    Task<bool> Signal(bool ignoreIfAlreadyExecution = false);

    /// <summary>
    /// Pauses the task until resume is called.
    /// </summary>
    /// <remarks>
    /// If the task is already paused this has no effect.
    /// </remarks>
    void Pause();

    /// <summary>
    /// Resumes execution of the task if it was paused.
    /// </summary>
    /// <remarks>
    /// If the task is already running this has no effect.
    /// </remarks>
    void Resume();

    /// <summary>
    /// Pauses the task for the provided duration and then automatically resumes the task.
    /// </summary>
    void PauseFor(TimeSpan period);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<int> WhenCompleted();
}