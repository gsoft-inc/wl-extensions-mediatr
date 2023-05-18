using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace GSoft.Extensions.MediatR;

internal static class TelemetryClientExtensions
{
    public static IOperationHolder<DependencyTelemetry> StartActivityAwareDependencyOperation(this TelemetryClient telemetryClient, object request)
    {
        if (Activity.Current is { } activity && TracingHelper.IsMediatorActivity(activity))
        {
            // When the current activity is our own Mediator activity created in our previous request tracing behavior,
            // then we use it to initialize the Application Insights operation.
            // The Application Insights SDK will take care of populating the parent-child relationship
            // and bridge the gap between our activity, its own internal activity and the AI operation telemetry.
            // Not doing that could cause some Application Insights AND OpenTelemetry spans to be orphans.
            var operation = telemetryClient.StartOperation<DependencyTelemetry>(activity);
            operation.Telemetry.Name = request.GetType().Name;

            return operation;
        }

        return telemetryClient.StartOperation<DependencyTelemetry>(request.GetType().Name);
    }
}