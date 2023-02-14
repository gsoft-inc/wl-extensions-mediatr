using System.Collections;
using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed class InMemoryLoggerTracker : IReadOnlyList<string>, ILoggerProvider, ILogger
{
    private readonly List<string> _logs;

    public InMemoryLoggerTracker()
    {
        this._logs = new List<string>();
    }

    public int Count
    {
        get
        {
            lock (this._logs)
            {
                return this._logs.Count;
            }
        }
    }

    public string this[int index]
    {
        get
        {
            lock (this._logs)
            {
                return this._logs[index];
            }
        }
    }

    public IEnumerator<string> GetEnumerator()
    {
        lock (this._logs)
        {
            return this._logs.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
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

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}