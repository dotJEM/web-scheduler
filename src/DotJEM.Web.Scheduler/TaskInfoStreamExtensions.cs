using System.Runtime.CompilerServices;
using DotJEM.ObservableExtensions.InfoStreams;

namespace DotJEM.Web.Scheduler;

public static class TaskInfoStreamExtensions
{
    public static void WriteTaskCompleted<TSource>(this IInfoStream<TSource> self, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        => self.WriteEvent(new TaskCompletedInfoStreamEvent(typeof(TSource), InfoLevel.INFO, message, callerMemberName, callerFilePath, callerLineNumber));
}