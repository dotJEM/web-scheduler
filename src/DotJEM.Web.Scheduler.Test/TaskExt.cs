namespace DotJEM.Web.Scheduler.Test;

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