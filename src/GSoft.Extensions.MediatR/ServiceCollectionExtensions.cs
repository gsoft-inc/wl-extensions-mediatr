using System.Reflection;
using GSoft.Extensions.MediatR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // Having our own extension methods to add MediatR protects consumers from future MediatR API breaking changes
    public static MediatorBuilder AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return new MediatorBuilder(services, assemblies, configure: null);
    }

    public static MediatorBuilder AddMediator(this IServiceCollection services, Action<MediatRServiceConfiguration>? configure, params Assembly[] assemblies)
    {
        return new MediatorBuilder(services, assemblies, configure);
    }

    public static MediatorBuilder AddMediator(this IServiceCollection services, IEnumerable<Assembly> assemblies, Action<MediatRServiceConfiguration>? configure)
    {
        return new MediatorBuilder(services, assemblies, configure);
    }

    public static MediatorBuilder AddMediator(this IServiceCollection services, params Type[] handlerAssemblyMarkerTypes)
    {
        return new MediatorBuilder(services, handlerAssemblyMarkerTypes, configure: null);
    }

    public static MediatorBuilder AddMediator(this IServiceCollection services, Action<MediatRServiceConfiguration>? configure, params Type[] handlerAssemblyMarkerTypes)
    {
        return new MediatorBuilder(services, handlerAssemblyMarkerTypes, configure);
    }

    public static MediatorBuilder AddMediator(this IServiceCollection services, IEnumerable<Type> handlerAssemblyMarkerTypes, Action<MediatRServiceConfiguration>? configure)
    {
        return new MediatorBuilder(services, handlerAssemblyMarkerTypes, configure);
    }
}