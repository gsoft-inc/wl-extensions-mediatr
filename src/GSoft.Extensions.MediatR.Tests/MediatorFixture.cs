using GSoft.Extensions.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR.Tests;

public sealed class MediatorFixture : BaseUnitFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<InMemoryActivityTracker>();

        services.AddSingleton<InMemoryLoggerTracker>();
        services.AddSingleton<ILoggerProvider>(x => x.GetRequiredService<InMemoryLoggerTracker>());
        services.AddLogging(x => x.AddFilter(typeof(MediatorBuilder).Assembly.GetName().Name!, LogLevel.Trace));

        // Add ApplicationInsights twice to verify we do not register duplicate behaviors
        services.AddMediator(typeof(MediatorFixture).Assembly)
            .AddApplicationInsights()
            .AddApplicationInsights();

        return services;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }
}