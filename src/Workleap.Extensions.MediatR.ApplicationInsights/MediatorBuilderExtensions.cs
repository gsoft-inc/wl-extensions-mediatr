using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.Extensions.MediatR;

public static class MediatorBuilderExtensions
{
    public static MediatorBuilder AddApplicationInsights(this MediatorBuilder builder)
    {
        // Insert ApplicationInsights behavior right AFTER OpenTelemetry tracing in order to record linked logging and track validation exceptions
        // ApplicationInsights also collects information from Activity.Current even if OpenTelemetry is not observed
        var tracingBehaviorIdx = -1;
        var streamTracingBehaviorIdx = -1;

        for (var i = 0; i < builder.Services.Count; i++)
        {
            var implementationType = builder.Services[i].ImplementationType;

            if (implementationType == typeof(RequestTracingBehavior<,>) && tracingBehaviorIdx == -1)
            {
                tracingBehaviorIdx = i;
            }
            else if (implementationType == typeof(StreamRequestTracingBehavior<,>) && streamTracingBehaviorIdx == -1)
            {
                streamTracingBehaviorIdx = i;
            }
            else if (implementationType == typeof(RequestApplicationInsightsBehavior<,>) || implementationType == typeof(StreamRequestApplicationInsightsBehavior<,>))
            {
                // ApplicationInsights behaviors already added
                return builder;
            }
        }

        if (tracingBehaviorIdx != -1)
        {
            builder.Services.Insert(tracingBehaviorIdx + 1, new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));

            if (streamTracingBehaviorIdx != -1)
            {
                // Inserting the ApplicationInsights request behavior offsets the next behavior insert
                streamTracingBehaviorIdx++;
            }
        }

        if (streamTracingBehaviorIdx != -1)
        {
            builder.Services.Insert(streamTracingBehaviorIdx + 1, new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));
        }

        return builder;
    }
}