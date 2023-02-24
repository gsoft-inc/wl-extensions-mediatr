using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace GSoft.Extensions.MediatR;

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
        var operation = this._telemetryClient.StartOperation<DependencyTelemetry>(request.GetType().Name);

        // Originating activity must be captured AFTER that the operation is created
        // Because ApplicationInsights SDK creates another intermediate Activity
        var originatingActivity = Activity.Current;

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