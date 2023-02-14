using System.Reflection;
using GSoft.Extensions.MediatR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static MediatorBuilder AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return new MediatorBuilder(services, assemblies);
    }
}