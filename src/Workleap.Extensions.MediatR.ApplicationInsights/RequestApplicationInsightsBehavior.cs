using System.Diagnostics;
using MediatR;
using Microsoft.ApplicationInsights;

namespace Workleap.Extensions.MediatR;

internal sealed class RequestApplicationInsightsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly TelemetryClient _telemetryClient;

    public RequestApplicationInsightsBehavior(TelemetryClient? telemetryClient = null)
    {
        this._telemetryClient = telemetryClient!;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next() : this.HandleWithTelemetry(request, next);
    }

    private async Task<TResponse> HandleWithTelemetry(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var operation = this._telemetryClient.StartActivityAwareDependencyOperation(request);

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

        try
        {
            operation.Telemetry.Type = ApplicationInsightsConstants.TelemetryType;
            var result = await next().ConfigureAwait(false);
            operation.Telemetry.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            operation.Telemetry.Properties.TryAdd(ApplicationInsightsConstants.Exception, ex.ToString());

            throw;
        }
        finally
        {
            // The dependency telemetry is sent when the operation is disposed
            if (originatingActivity == null)
            {
                operation.Dispose();
            }
            else
            {
                // Attach the telemetry to the originating activity
                originatingActivity.ExecuteAsCurrentActivity(operation, static x => x.Dispose());
            }
        }
    }
}