using System.Diagnostics;

namespace Workleap.Extensions.MediatR;

/// <summary>
/// Stopwatch implementation that's allocation free and better to profile/time large quantity of code
/// </summary>
/// <seealso cref="https://github.com/dotnet/aspnetcore/blob/v7.0.0/src/Shared/ValueStopwatch/ValueStopwatch.cs"/>
internal readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        this._startTimestamp = startTimestamp;
    }

    public bool IsActive => this._startTimestamp != 0;

    public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
        // So it being 0 is a clear indication of default(ValueStopwatch)
        if (!this.IsActive)
        {
            throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");
        }

        var end = Stopwatch.GetTimestamp();
        var timestampDelta = end - this._startTimestamp;
        var ticks = (long)(TimestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }
}