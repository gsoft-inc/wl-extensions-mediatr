using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.Extensions.MediatR;

public sealed class MediatorBuilder
{
    internal MediatorBuilder(IServiceCollection services, IEnumerable<Assembly> assemblies, Action<MediatRServiceConfiguration>? configure)
    {
        void RegisterAssemblies(MediatRServiceConfiguration configuration)
        {
            foreach (var assembly in assemblies)
            {
                configuration.RegisterServicesFromAssembly(assembly);
            }
        }

        EnsureAddMediatorIsOnlyCalledOnce(services);
        services.AddMediatR(ConfigurationFactory(RegisterAssemblies, configure));
        this.Services = services;
    }

    internal MediatorBuilder(IServiceCollection services, IEnumerable<Type> handlerAssemblyMarkerTypes, Action<MediatRServiceConfiguration>? configure)
    {
        void RegisterAssembliesOfTypes(MediatRServiceConfiguration configuration)
        {
            foreach (var type in handlerAssemblyMarkerTypes)
            {
                configuration.RegisterServicesFromAssemblyContaining(type);
            }
        }

        EnsureAddMediatorIsOnlyCalledOnce(services);
        services.AddMediatR(ConfigurationFactory(RegisterAssembliesOfTypes, configure));
        this.Services = services;
    }

    public IServiceCollection Services { get; }

    private static void EnsureAddMediatorIsOnlyCalledOnce(IServiceCollection services)
    {
        // If a service descriptor references one of our internal behaviors, it means we already executed our AddMediator method.
        // We prevent this because MediatR's "AddMediatR" method doesn't completely check for duplicate registration of behaviors
        // and might register duplicate behaviors if someone call this method multiple times
        // This will be addressed in https://github.com/jbogard/MediatR/pull/860
        if (services.Any(x => x.ImplementationType == typeof(RequestTracingBehavior<,>)))
        {
            throw new InvalidOperationException(nameof(ServiceCollectionExtensions.AddMediator) + " cannot be called multiple times");
        }
    }

    private static Action<MediatRServiceConfiguration> ConfigurationFactory(Action<MediatRServiceConfiguration> configureRegistrations, Action<MediatRServiceConfiguration>? userDefinedConfigure)
    {
        void Configure(MediatRServiceConfiguration configuration)
        {
            ConfigureDefaultConfiguration(configuration);

            configureRegistrations(configuration);

            // Allow developers to override default configuration if needed
            userDefinedConfigure?.Invoke(configuration);
        }

        return Configure;
    }

    private static void ConfigureDefaultConfiguration(MediatRServiceConfiguration configuration)
    {
        // By default, register IMediator as a singleton, we don't want to create a new instance of Mediator every time
        // Request handlers are still registered as transient though
        configuration.Lifetime = ServiceLifetime.Singleton;

        // Register open singleton behaviors, invoked for any type of request
        // OpenTelemetry tracing first
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestTracingBehavior<,>), ServiceLifetime.Singleton));
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestTracingBehavior<,>), ServiceLifetime.Singleton));

        // Then logging, so the logs can be linked to the parent traces
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>), ServiceLifetime.Singleton));
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestLoggingBehavior<,>), ServiceLifetime.Singleton));

        // Then validation so errors can be recorded by tracing and logging
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>), ServiceLifetime.Singleton));
        configuration.BehaviorsToRegister.Add(new ServiceDescriptor(typeof(IStreamPipelineBehavior<,>), typeof(StreamRequestValidationBehavior<,>), ServiceLifetime.Singleton));
    }
}