using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSoft.Extensions.MediatR;

internal sealed class StreamRequestLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly ILogger<StreamRequestLoggingBehavior<TRequest, TResponse>> _logger;

    public StreamRequestLoggingBehavior(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        this._logger = loggerFactory.CreateLogger<StreamRequestLoggingBehavior<TRequest, TResponse>>();
    }

    public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var originalActivity = Activity.Current;
        var requestName = request.GetType().Name;

        this._logger.StreamRequestStarted(requestName);
        var watch = Stopwatch.StartNew();

        IAsyncEnumerator<TResponse> resultsEnumerator;

        try
        {
            resultsEnumerator = next().GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            watch.Stop();

            if (originalActivity == null)
            {
                this._logger.StreamRequestFailed(ex, requestName, watch.Elapsed.TotalSeconds);
            }
            else
            {
                // Make sure the logs being sent are attached to the originating activity
                originalActivity.ExecuteAsCurrentActivity(new FailedLoggerState(this._logger, requestName, watch.Elapsed.TotalSeconds, ex), static x =>
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
                    watch.Stop();

                    if (originalActivity == null)
                    {
                        this._logger.StreamRequestFailed(ex, requestName, watch.Elapsed.TotalSeconds);
                    }
                    else
                    {
                        // Make sure the logs being sent are attached to the originating activity
                        originalActivity.ExecuteAsCurrentActivity(new FailedLoggerState(this._logger, requestName, watch.Elapsed.TotalSeconds, ex), static x =>
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

        watch.Stop();

        if (originalActivity == null)
        {
            this._logger.StreamRequestSucceeded(requestName, watch.Elapsed.TotalSeconds);
        }
        else
        {
            // Make sure the logs being sent are attached to the originating activity
            originalActivity.ExecuteAsCurrentActivity(new SuccessfulLoggerState(this._logger, requestName, watch.Elapsed.TotalSeconds), static x =>
            {
                x.Logger.StreamRequestSucceeded(x.RequestName, x.Elapsed);
            });
        }
    }

    // Using a closure to capture these properties would have create a hidden reference and unnecessary allocations
    private readonly struct SuccessfulLoggerState
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

    // Using a closure to capture these properties would have create a hidden reference and unnecessary allocations
    private readonly struct FailedLoggerState
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
}