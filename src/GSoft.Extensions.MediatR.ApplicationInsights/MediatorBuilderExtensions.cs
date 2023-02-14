using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSoft.Extensions.MediatR.ApplicationInsights;

public static class MediatorBuilderExtensions
{
    public static MediatorBuilder AddApplicationInsights(this MediatorBuilder builder)
    {
        // TODO Insert these behaviors AFTER the OpenTelemetry tracing behaviors and BEFORE the logging behaviors in order to record telemetry when validation fails
        builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));
        builder.Services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestApplicationInsightsBehavior<,>), ServiceLifetime.Singleton));

        return builder;
    }
}