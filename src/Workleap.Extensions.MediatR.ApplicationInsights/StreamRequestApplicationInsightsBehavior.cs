using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.ApplicationInsights;

namespace Workleap.Extensions.MediatR;

internal sealed class StreamRequestApplicationInsightsBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly TelemetryClient _telemetryClient;

    public StreamRequestApplicationInsightsBehavior(TelemetryClient? telemetryClient = null)
    {
        this._telemetryClient = telemetryClient!;
    }

    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next() : this.HandleWithTelemetryAsync(request, next, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> HandleWithTelemetryAsync(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var operation = this._telemetryClient.StartActivityAwareDependencyOperation(request);

        // After starting an operation, the Application Insights SDK creates its own internal activity, and it becomes the current activity.
        // When we dispose an operation to send the telemetry, the Application Insights SDK expects the current activity to still be its own internal activity.
        // However, IAsyncEnumerable is a special type, and its use can span across multiple activities.
        // For instance, it can still be enumerated long after being returned by the mediator.
        // We capture this AI internal activity to temporarily set it as the current one when disposing the operation.
        var applicationInsightsInternalActivity = Activity.Current;

        try
        {
            operation.Telemetry.Type = ApplicationInsightsConstants.TelemetryType;

            IAsyncEnumerator<TResponse> resultsEnumerator;

            try
            {
                resultsEnumerator = next().GetAsyncEnumerator(cancellationToken);
            }
            catch (Exception ex)
            {
                operation.Telemetry.Success = false;
                operation.Telemetry.Properties.TryAdd(ApplicationInsightsConstants.Exception, ex.ToString());

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
                        operation.Telemetry.Success = false;
                        operation.Telemetry.Properties.TryAdd(ApplicationInsightsConstants.Exception, ex.ToString());

                        throw;
                    }

                    if (hasNext)
                    {
                        yield return resultsEnumerator.Current;
                    }
                }
            }

            operation.Telemetry.Success = true;
        }
        finally
        {
            // The dependency telemetry is sent when the operation is disposed
            if (applicationInsightsInternalActivity == null)
            {
                operation.Dispose();
            }
            else
            {
                // Attach the telemetry to the original Application Insights internal activity
                applicationInsightsInternalActivity.ExecuteAsCurrentActivity(operation, static x => x.Dispose());
            }
        }
    }
}