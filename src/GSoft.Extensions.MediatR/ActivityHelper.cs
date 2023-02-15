using System.Diagnostics;

namespace GSoft.Extensions.MediatR;

internal static class ActivityHelper
{
    public static void ExecuteAsCurrentActivity<TState>(this Activity newCurrentActivity, TState state, Action<TState> action)
    {
        // There is no guarantee that Activity.Current is the same than at the beginning or the method because of the async enumerable flow
        // Temporarily re-attach the original activity when disposing the operation item, ApplicationInsights SDK will use it
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