using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace GSoft.Extensions.MediatR;

internal sealed class RequestApplicationInsightsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TelemetryClient? _telemetryClient;

    public RequestApplicationInsightsBehavior(TelemetryClient? telemetryClient = null)
    {
        this._telemetryClient = telemetryClient;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next() : this.HandleWithTelemetry(request, next);
    }

    private async Task<TResponse> HandleWithTelemetry(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        using (var operation = this._telemetryClient.StartOperation<DependencyTelemetry>(request.GetType().Name))
        {
            operation.Telemetry.Type = ApplicationInsightsConstants.TelemetryType;

            try
            {
                var result = await next().ConfigureAwait(false);
                operation.Telemetry.Success = true;

                return result;
            }
            catch (Exception ex)
            {
                operation.Telemetry.Success = false;
                this._telemetryClient?.TrackException(ex);
                throw;
            }
        }
    }
}