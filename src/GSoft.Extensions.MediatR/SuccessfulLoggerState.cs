using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR;

// This struct is used in a static context, to avoid allocations induced by closures
internal readonly struct SuccessfulLoggerState
{
    public SuccessfulLoggerState(ILogger logger, string requestName, double elapsed)
    {
        this.Logger = logger;
        this.RequestName = requestName;
        this.Elapsed = elapsed;
    }

    public ILogger Logger { get; }

    public string RequestName { get; }

    public double Elapsed { get; }
}