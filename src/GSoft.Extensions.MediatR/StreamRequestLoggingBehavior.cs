using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSoft.Extensions.MediatR;

internal sealed class StreamRequestLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<StreamRequestLoggingBehavior<TRequest, TResponse>> _logger;

    public StreamRequestLoggingBehavior(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        this._logger = loggerFactory.CreateLogger<StreamRequestLoggingBehavior<TRequest, TResponse>>();
    }

    public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var originatingActivity = Activity.Current;
        var requestName = request.GetType().Name;

        this._logger.StreamRequestStarted(requestName);
        var watch = ValueStopwatch.StartNew();

        IAsyncEnumerator<TResponse> resultsEnumerator;

        try
        {
            resultsEnumerator = next().GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            if (originatingActivity == null)
            {
                this._logger.StreamRequestFailed(ex, requestName, watch.GetElapsedTime().TotalSeconds);
            }
            else
            {
                // Make sure the logs being sent are attached to the originating activity
                originatingActivity.ExecuteAsCurrentActivity(new FailedLoggerState(this._logger, requestName, watch.GetElapsedTime().TotalSeconds, ex), static x =>
                {
                    x.Logger.StreamRequestFailed(x.Exception, x.RequestName, x.Elapsed);
                });
            }

            throw;
        }

        await using (resultsEnumerator.ConfigureAwait(false))
        {
            var hasNext = true;

            while (hasNext)
            {
                try
                {
                    hasNext = await resultsEnumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (originatingActivity == null)
                    {
                        this._logger.StreamRequestFailed(ex, requestName, watch.GetElapsedTime().TotalSeconds);
                    }
                    else
                    {
                        // Make sure the logs being sent are attached to the originating activity
                        originatingActivity.ExecuteAsCurrentActivity(new FailedLoggerState(this._logger, requestName, watch.GetElapsedTime().TotalSeconds, ex), static x =>
                        {
                            x.Logger.StreamRequestFailed(x.Exception, x.RequestName, x.Elapsed);
                        });
                    }

                    throw;
                }

                if (hasNext)
                {
                    yield return resultsEnumerator.Current;
                }
            }
        }

        if (originatingActivity == null)
        {
            this._logger.StreamRequestSucceeded(requestName, watch.GetElapsedTime().TotalSeconds);
        }
        else
        {
            // Make sure the logs being sent are attached to the originating activity
            originatingActivity.ExecuteAsCurrentActivity(new SuccessfulLoggerState(this._logger, requestName, watch.GetElapsedTime().TotalSeconds), static x =>
            {
                x.Logger.StreamRequestSucceeded(x.RequestName, x.Elapsed);
            });
        }
    }
}