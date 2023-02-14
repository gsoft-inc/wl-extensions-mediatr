using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSoft.Extensions.MediatR;

public sealed class MediatorBuilder
{
    internal MediatorBuilder(IServiceCollection services, IEnumerable<Assembly> assemblies, Action<MediatRServiceConfiguration>? configure)
    {
        services.AddMediatR(assemblies, ConfigurationFactory(configure));
        RegisterBehaviors(services);
        this.Services = services;
    }

    internal MediatorBuilder(IServiceCollection services, IEnumerable<Type> handlerAssemblyMarkerTypes, Action<MediatRServiceConfiguration>? configure)
    {
        services.AddMediatR(handlerAssemblyMarkerTypes, ConfigurationFactory(configure));
        RegisterBehaviors(services);
        this.Services = services;
    }

    public IServiceCollection Services { get; }

    private static Action<MediatRServiceConfiguration> ConfigurationFactory(Action<MediatRServiceConfiguration>? userDefinedConfigure)
    {
        void Configure(MediatRServiceConfiguration configuration)
        {
            userDefinedConfigure?.Invoke(configuration);

            // Force IMediator to be registered as singleton, we don't want to create a new instance of Mediator every time
            // Request handlers are still registered as transient though
            configuration.AsSingleton();
        }

        return Configure;
    }

    private static void RegisterBehaviors(IServiceCollection services)
    {
        // OpenTelemetry tracing first
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestTracingBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestTracingBehavior<,>), ServiceLifetime.Singleton));

        // Then logging, so the logs can be linked to the parent traces
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestLoggingBehavior<,>), ServiceLifetime.Singleton));

        // Then validation so errors can be recorded by tracing and logging
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>), ServiceLifetime.Singleton));
        services.TryAddEnumerable(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>), ServiceLifetime.Singleton));
    }
}