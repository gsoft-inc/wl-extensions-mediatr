using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Workleap.Extensions.MediatR.Tests;

internal sealed class InMemoryLoggerTracker : ILoggerProvider, ILogger
{
    private readonly List<LogRecord> _logs;

    public InMemoryLoggerTracker()
    {
        this._logs = new List<LogRecord>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (this._logs)
        {
            this._logs.Add(new LogRecord(formatter(state, exception), Activity.Current?.Id));
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new NoopDisposable();
    }

    public void Dispose()
    {
    }

    public void AssertRequestSuccessful(string startsWith)
    {
        lock (this._logs)
        {
            Assert.Equal(2, this._logs.Count);
            Assert.StartsWith(startsWith + " started", this._logs[0].Text);
            Assert.StartsWith(startsWith + " ended successfully", this._logs[1].Text);
            Assert.NotNull(this._logs[0].ActivityId);
            Assert.Equal(this._logs[0].ActivityId, this._logs[1].ActivityId);
        }
    }

    public void AssertRequestFailed(string startsWith)
    {
        lock (this._logs)
        {
            Assert.Equal(2, this._logs.Count);
            Assert.StartsWith(startsWith + " started", this._logs[0].Text);
            Assert.StartsWith(startsWith + " failed after", this._logs[1].Text);
            Assert.NotNull(this._logs[0].ActivityId);
            Assert.Equal(this._logs[0].ActivityId, this._logs[1].ActivityId);
        }
    }

    private sealed record LogRecord(string Text, string? ActivityId);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}