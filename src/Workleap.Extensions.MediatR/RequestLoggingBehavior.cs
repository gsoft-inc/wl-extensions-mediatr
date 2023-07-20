using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Workleap.Extensions.MediatR;

internal sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        this._logger = loggerFactory.CreateLogger<RequestLoggingBehavior<TRequest, TResponse>>();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var originatingActivity = Activity.Current;
        var requestName = request.GetType().Name;

        this._logger.RequestStarted(requestName);
        var watch = ValueStopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            if (originatingActivity == null)
            {
                this._logger.RequestSucceeded(requestName, watch.GetElapsedTime().TotalSeconds);
            }
            else
            {
                // Make sure the logs being sent are attached to the originating activity
                originatingActivity.ExecuteAsCurrentActivity(new SuccessfulLoggerState(this._logger, requestName, watch.GetElapsedTime().TotalSeconds), static x =>
                {
                    x.Logger.RequestSucceeded(x.RequestName, x.Elapsed);
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            if (originatingActivity == null)
            {
                this._logger.RequestFailed(ex, requestName, watch.GetElapsedTime().TotalSeconds);
            }
            else
            {
                // Make sure the logs being sent are attached to the originating activity
                originatingActivity.ExecuteAsCurrentActivity(new FailedLoggerState(this._logger, requestName, watch.GetElapsedTime().TotalSeconds, ex), static x =>
                {
                    x.Logger.RequestFailed(x.Exception, x.RequestName, x.Elapsed);
                });
            }

            throw;
        }
    }
}