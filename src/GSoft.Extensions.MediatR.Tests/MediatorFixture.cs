using GSoft.Extensions.MediatR.ApplicationInsights;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR.Tests;

public sealed class MediatorFixture : BaseUnitFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddLogging(x => x.AddFilter(typeof(MediatorBuilder).Assembly.GetName().Name!, LogLevel.Trace));
        services.AddMediator(typeof(MediatorFixture).Assembly).AddApplicationInsights();

        return services;
    }
}