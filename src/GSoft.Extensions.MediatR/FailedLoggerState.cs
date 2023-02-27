using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR;

// This struct is used in a static context, to avoid allocations induced by closures
internal readonly struct FailedLoggerState
{
    public FailedLoggerState(ILogger logger, string requestName, double elapsed, Exception exception)
    {
        this.Logger = logger;
        this.RequestName = requestName;
        this.Elapsed = elapsed;
        this.Exception = exception;
    }

    public ILogger Logger { get; }

    public string RequestName { get; }

    public double Elapsed { get; }

    public Exception Exception { get; }
}