using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed class InMemoryTelemetryTracker : ITelemetryChannel
{
    private readonly List<ITelemetry> _telemetry;

    public InMemoryTelemetryTracker()
    {
        this._telemetry = new List<ITelemetry>();
    }

    public bool? DeveloperMode { get; set; }

    public string EndpointAddress { get; set; } = string.Empty;

    public void Dispose()
    {
    }

    public void Send(ITelemetry item)
    {
        lock (this._telemetry)
        {
            this._telemetry.Add(item);
        }
    }

    public void Flush()
    {
    }

    public void AssertRequestSuccessful(string requestName)
    {
        lock (this._telemetry)
        {
            var dependency = Assert.IsType<DependencyTelemetry>(Assert.Single(this._telemetry));

            Assert.True(dependency.Success);
            Assert.Equal(requestName, dependency.Name);
            Assert.Equal("Mediator", dependency.Type);
        }
    }

    public void AssertRequestFailed(string requestName, Exception exception)
    {
        lock (this._telemetry)
        {
            Assert.Equal(2, this._telemetry.Count);
            var dependencyTelemetry = Assert.Single(this._telemetry.OfType<DependencyTelemetry>());
            var exceptionTelemetry = Assert.Single(this._telemetry.OfType<ExceptionTelemetry>());

            Assert.False(dependencyTelemetry.Success);
            Assert.Equal(requestName, dependencyTelemetry.Name);
            Assert.Equal("Mediator", dependencyTelemetry.Type);

            Assert.Same(exception, exceptionTelemetry.Exception);
        }
    }
}