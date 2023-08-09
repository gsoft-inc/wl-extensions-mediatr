using System.Diagnostics;

namespace Workleap.Extensions.MediatR;

internal static class ActivityHelperExtensions
{
    public static void ExecuteAsCurrentActivity<TState>(this Activity newCurrentActivity, TState state, Action<TState> action)
    {
        var oldCurrentActivity = Activity.Current;
        Activity.Current = newCurrentActivity;

        try
        {
            action(state);
        }
        finally
        {
            Activity.Current = oldCurrentActivity;
        }
    }
}