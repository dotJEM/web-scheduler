using System;
using DotJEM.ObservableExtensions.InfoStreams;

namespace DotJEM.Web.Scheduler;

public class TaskCompletedInfoStreamEvent : InfoStreamEvent
{
    public TaskCompletedInfoStreamEvent(Type source, InfoLevel level, string message, string callerMemberName, string callerFilePath, int callerLineNumber)
        : base(source, level, message, callerMemberName, callerFilePath, callerLineNumber)
    {
    }
}