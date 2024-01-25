using System;
using System.Threading;

namespace DotJEM.Web.Scheduler.Abstractions;

/// <summary>
/// Simple proxy implementation of <see cref="IThreadPool"/> targeting the <see cref="ThreadPool"/> class. Abstraction meant to be able to be used in testing.
/// </summary>
public class ThreadPoolProxy : IThreadPool
{
    /// <summary>
    /// Delegates to <see cref="ThreadPool.RegisterWaitForSingleObject(System.Threading.WaitHandle,System.Threading.WaitOrTimerCallback,object,int,bool)"/>
    /// </summary>
    public RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle handle, WaitOrTimerCallback callback, object state, TimeSpan timeout, bool executeOnlyOnce)
    {
        return ThreadPool.RegisterWaitForSingleObject(handle, callback, state, timeout, executeOnlyOnce);
    }
}