using System.Collections;
using System.Diagnostics;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed class InMemoryActivityTracker : IReadOnlyList<Activity>, IDisposable
{
    private static readonly HashSet<InMemoryActivityTracker> ActiveTrackers = new HashSet<InMemoryActivityTracker>();

    private readonly List<Activity> _activities;

    static InMemoryActivityTracker()
    {
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs?source=recommendations#add-code-to-collect-the-traces
        var staticListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "GSoft.Extensions.MediatR",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (ActiveTrackers)
                {
                    foreach (var tracker in ActiveTrackers)
                    {
                        tracker.TrackActivity(activity);
                    }
                }
            },
        };

        ActivitySource.AddActivityListener(staticListener);
    }

    public InMemoryActivityTracker()
    {
        this._activities = new List<Activity>();

        lock (ActiveTrackers)
        {
            ActiveTrackers.Add(this);
        }
    }

    public int Count
    {
        get
        {
            lock (this._activities)
            {
                return this._activities.Count;
            }
        }
    }

    public Activity this[int index]
    {
        get
        {
            lock (this._activities)
            {
                return this._activities[index];
            }
        }
    }

    private void TrackActivity(Activity activity)
    {
        lock (this._activities)
        {
            this._activities.Add(activity);
        }
    }

    public void Dispose()
    {
        lock (ActiveTrackers)
        {
            ActiveTrackers.Remove(this);
        }
    }

    public IEnumerator<Activity> GetEnumerator()
    {
        lock (this._activities)
        {
            return this._activities.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}