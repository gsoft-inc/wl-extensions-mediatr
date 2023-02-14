using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.Extensions.MediatR;

public static class MediatorBuilderExtensions
{
    public static MediatorBuilder AddApplicationInsights(this MediatorBuilder builder)
    {
        // Insert ApplicationInsights behavior right before OpenTelemetry tracing in order to record linked logging and track validation exceptions
        var tracingBehaviorIdx = -1;

        for (var i = 0; i < builder.Services.Count; i++)
        {
            var implementationType = builder.Services[i].ImplementationType;
            if (implementationType == typeof(RequestTracingBehavior<,>))
            {
                tracingBehaviorIdx = i;
                break;
            }

            if (implementationType == typeof(RequestApplicationInsightsBehavior<,>))
            {
                // Behavior already added
                return builder;
            }
        }

        if (tracingBehaviorIdx != -1)
        {
            builder.Services.Insert(tracingBehaviorIdx, new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));
            builder.Services.Insert(tracingBehaviorIdx, new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));
        }

        return builder;
    }
}