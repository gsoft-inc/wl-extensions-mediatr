using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed class InMemoryLoggerTracker : ILoggerProvider, ILogger
{
    private readonly List<string> _logs;

    public InMemoryLoggerTracker()
    {
        this._logs = new List<string>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (this._logs)
        {
            this._logs.Add(formatter(state, exception));
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
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
            Assert.Single(this._logs, x => x.StartsWith(startsWith + " started"));
            Assert.Single(this._logs, x => x.StartsWith(startsWith + " ended successfully"));
        }
    }

    public void AssertRequestFailed(string startsWith)
    {
        lock (this._logs)
        {
            Assert.Equal(2, this._logs.Count);
            Assert.Single(this._logs, x => x.StartsWith(startsWith + " started"));
            Assert.Single(this._logs, x => x.StartsWith(startsWith + " failed after"));
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}