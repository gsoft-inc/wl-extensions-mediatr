using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSoft.Extensions.MediatR;

internal sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        this._logger = loggerFactory.CreateLogger<RequestLoggingBehavior<TRequest, TResponse>>();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;

        this._logger.RequestStarted(requestName);
        var watch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);

            watch.Stop();
            this._logger.RequestSucceeded(requestName, watch.Elapsed.TotalSeconds);

            return response;
        }
        catch (Exception ex)
        {
            watch.Stop();
            this._logger.RequestFailed(ex, requestName, watch.Elapsed.TotalSeconds);

            throw;
        }
    }
}