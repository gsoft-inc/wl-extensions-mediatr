using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using MediatR.Registration;
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

        this.Services = services;

        EnsureAddMediatorIsOnlyCalledOnce(services);
        services.AddMediatR(ConfigurationFactory(RegisterAssemblies, configure));
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

        this.Services = services;

        EnsureAddMediatorIsOnlyCalledOnce(services);
        services.AddMediatR(ConfigurationFactory(RegisterAssembliesOfTypes, configure));
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

            // TEMPORARY HACK UNTIL AUTOMATIC REGISTRATION OF PRE/POST PROCESSORS IS FIXED
            // See: https://github.com/jbogard/MediatR/pull/989#issuecomment-1883574379
            RegisterPreAndPostNonGenericClosedProcessors(configuration);

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

    private static readonly PropertyInfo? AssembliesToRegisterPropertyInfo = typeof(MediatRServiceConfiguration).GetProperty(
        name: "AssembliesToRegister",
        bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        returnType: typeof(List<Assembly>),
        types: Array.Empty<Type>(),
        modifiers: null);

    private static readonly MethodInfo? ConnectImplementationsToTypesClosingMethodInfo = typeof(ServiceRegistrar).GetMethod(
        name: "ConnectImplementationsToTypesClosing",
        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
        binder: null,
        types: new[] { typeof(Type), typeof(IServiceCollection), typeof(IEnumerable<Assembly>), typeof(bool), typeof(MediatRServiceConfiguration), typeof(CancellationToken) },
        modifiers: null);

    private static void RegisterPreAndPostNonGenericClosedProcessors(MediatRServiceConfiguration configuration)
    {
        if (AssembliesToRegisterPropertyInfo == null || ConnectImplementationsToTypesClosingMethodInfo == null)
        {
            // If this happens, it means MediatR internals have changed. Maybe this hack isn't needed anymore?
            // In any case, our tests will reflect if this is still needed or not
            return;
        }

        // We basically execute the same code that worked in MediatR 12.0.1 when pre/post processors were automatically registered:
        // https://github.com/jbogard/MediatR/blob/v12.0.1/src/MediatR/Registration/ServiceRegistrar.cs#L21-L22
        var assembliesToRegister = (List<Assembly>)AssembliesToRegisterPropertyInfo.GetValue(configuration)!;

        // Populating "RequestPreProcessorsToRegister" will make MediatR register the pre-processor behavior that will invoke the processors:
        // https://github.com/jbogard/MediatR/blob/v12.2.0/src/MediatR/Registration/ServiceRegistrar.cs#L247-L251
        var preProcessorServiceDescriptors = new ServiceCollection();
        ConnectImplementationsToTypesClosingMethodInfo.Invoke(obj: null, parameters: new object?[]
        {
            typeof(IRequestPreProcessor<>), preProcessorServiceDescriptors, assembliesToRegister, true, configuration, CancellationToken.None,
        });
        configuration.RequestPreProcessorsToRegister.AddRange(preProcessorServiceDescriptors);

        // Same thing for the post-processors:
        // https://github.com/jbogard/MediatR/blob/v12.2.0/src/MediatR/Registration/ServiceRegistrar.cs#L253-L257
        var postProcessorServiceDescriptors = new ServiceCollection();
        ConnectImplementationsToTypesClosingMethodInfo.Invoke(obj: null, parameters: new object?[]
        {
            typeof(IRequestPostProcessor<,>), postProcessorServiceDescriptors, assembliesToRegister, true, configuration, CancellationToken.None,
        });
        configuration.RequestPostProcessorsToRegister.AddRange(postProcessorServiceDescriptors);
    }
}