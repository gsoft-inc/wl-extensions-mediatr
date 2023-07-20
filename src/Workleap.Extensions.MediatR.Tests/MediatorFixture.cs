using GSoft.Extensions.Xunit;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Workleap.Extensions.MediatR.Tests;

public sealed class MediatorFixture : BaseUnitFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // OpenTelemetry test dependencies
        services.AddSingleton<InMemoryActivityTracker>();

        // Logging test dependencies
        services.AddSingleton<InMemoryLoggerTracker>();
        services.AddSingleton<ILoggerProvider>(x => x.GetRequiredService<InMemoryLoggerTracker>());
        services.AddLogging(x => x.AddFilter(typeof(MediatorBuilder).Assembly.GetName().Name!, LogLevel.Trace));

        // ApplicationInsights test dependencies
        services.AddSingleton<InMemoryTelemetryTracker>();
        services.AddSingleton(x =>
        {
            var configuration = new TelemetryConfiguration("fake-instrumentation-key", x.GetRequiredService<InMemoryTelemetryTracker>());
            return new TelemetryClient(configuration);
        });

        // Add MediatR with ApplicationInsights instrumentation
        var builder = services.AddMediator(typeof(MediatorFixture).Assembly).AddApplicationInsights();

        // We only want unique instances of ApplicationInsights behaviors in MediatR, so there is a test that ensures
        // that only one instance of these behaviors is registered. Calling this method below multiple times as we do
        // shouldn't create duplicate registered behaviors:
        builder.AddApplicationInsights().AddApplicationInsights();

        return services;
    }
}