using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace GSoft.Extensions.MediatR;

internal sealed class StreamRequestApplicationInsightsBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly TelemetryClient? _telemetryClient;

    public StreamRequestApplicationInsightsBehavior(TelemetryClient? telemetryClient = null)
    {
        this._telemetryClient = telemetryClient;
    }

    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        return this._telemetryClient == null ? next() : this.HandleWithTelemetryAsync(request, next, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> HandleWithTelemetryAsync(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestName = request.GetType().Name;

        using (var operation = this._telemetryClient.StartOperation<DependencyTelemetry>(requestName))
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
                this._telemetryClient?.TrackException(ex);
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
                        this._telemetryClient?.TrackException(ex);
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
    }
}