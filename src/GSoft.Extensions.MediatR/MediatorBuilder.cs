using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSoft.Extensions.MediatR;

public sealed class MediatorBuilder
{
    internal MediatorBuilder(IServiceCollection services, Assembly[] assemblies)
    {
        services.AddMediatR(Configure, assemblies);

        // OpenTelemetry tracing first
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestTracingBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestTracingBehavior<,>), ServiceLifetime.Singleton));

        // Then logging, so the logs can be linked to the parent traces
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestLoggingBehavior<,>), ServiceLifetime.Singleton));

        // Then validation so errors can be recorded by tracing and logging
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>), ServiceLifetime.Singleton));

        this.Services = services;
    }

    public IServiceCollection Services { get; }

    private static void Configure(MediatRServiceConfiguration options)
    {
        // The mediator instance is registered as singleton but the handlers are still transient
        options.AsSingleton();
    }
}