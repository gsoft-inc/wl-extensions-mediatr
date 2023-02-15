using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

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
        var originalActivity = Activity.Current;

        var operation = this._telemetryClient.StartOperation<DependencyTelemetry>(request.GetType().Name);

        try
        {
            var operationId = GetCompatibleApplicationInsightsOperationId(originalActivity);
            if (operationId != null)
            {
                operation.Telemetry.Id = operationId;
            }

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
        finally
        {
            DisposeWithCurrentActivity(originalActivity, operation);
        }
    }

    private static string? GetCompatibleApplicationInsightsOperationId(Activity? activity)
    {
        if (activity == null)
        {
            return null;
        }

        // Consolidate ApplicationInsights operation with current activity, otherwise the operation might not be tracked when we dispose it
        // See other comment at the end of this method, when dispose the operation
        // https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.21.0/BASE/src/Microsoft.ApplicationInsights/TelemetryClientExtensions.cs#L59-L77
        // https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.21.0/BASE/src/Microsoft.ApplicationInsights/Extensibility/Implementation/OperationHolder.cs#L80-L83
        // https://github.com/microsoft/ApplicationInsights-dotnet/blob/2.21.0/BASE/src/Microsoft.ApplicationInsights/Extensibility/Implementation/OperationHolder.cs#L80-L83
        return activity.IdFormat switch
        {
            ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
            ActivityIdFormat.Hierarchical => activity.Id,
            _ => null,
        };
    }

    private static void DisposeWithCurrentActivity(Activity? activity, IOperationHolder<DependencyTelemetry> operation)
    {
        // There is no guarantee that Activity.Current is the same than at the beginning or the method because of the async enumerable flow
        // Temporarily re-attach the original activity when disposing the operation item, ApplicationInsights SDK will use it
        var currentActivity = Activity.Current;
        Activity.Current = activity;

        try
        {
            operation.Dispose();
        }
        finally
        {
            Activity.Current = currentActivity;
        }
    }
}