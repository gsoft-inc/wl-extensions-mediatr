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
            this._logger.StreamRequestFailed(ex, requestName, watch.Elapsed.TotalSeconds);
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
                    this._logger.StreamRequestFailed(ex, requestName, watch.Elapsed.TotalSeconds);
                    throw;
                }

                if (hasNext)
                {
                    yield return resultsEnumerator.Current;
                }
            }
        }

        watch.Stop();
        this._logger.StreamRequestSucceeded(requestName, watch.Elapsed.TotalSeconds);
    }
}