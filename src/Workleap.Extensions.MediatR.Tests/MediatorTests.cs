using System.Diagnostics.CodeAnalysis;
using GSoft.Extensions.Xunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.Extensions.MediatR.Tests;

[SuppressMessage("ReSharper", "ConvertToLocalFunction", Justification = "Delegates as variables instead of local functions in this test code improves readability")]
public sealed class MediatorTests : BaseUnitTest<MediatorFixture>
{
    private readonly IMediator _mediator;
    private readonly InMemoryLoggerTracker _logs;
    private readonly InMemoryActivityTracker _activities;
    private readonly InMemoryTelemetryTracker _telemetry;

    public MediatorTests(MediatorFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._mediator = this.Services.GetRequiredService<IMediator>();
        this._logs = this.Services.GetRequiredService<InMemoryLoggerTracker>();
        this._activities = this.Services.GetRequiredService<InMemoryActivityTracker>();
        this._telemetry = this.Services.GetRequiredService<InMemoryTelemetryTracker>();
    }

    [Fact]
    public async Task Send_Query_Works_When_Handler_Is_Succesful()
    {
        var result = await this._mediator.SendAsync(new SampleQuery("world", IsSuccessful: true), CancellationToken.None);
        Assert.Equal("Hello world!", result);

        this._logs.AssertRequestSuccessful("Request SampleQuery");
        this._activities.AssertRequestSuccessful("SampleQuery");
        this._telemetry.AssertRequestSuccessful("SampleQuery");
    }

    [Fact]
    public async Task Send_Query_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.SendAsync(new SampleQuery("world", IsSuccessful: false), CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        this._logs.AssertRequestFailed("Request SampleQuery");
        this._activities.AssertRequestFailed("SampleQuery", exception);
        this._telemetry.AssertRequestFailed("SampleQuery", exception);
    }

    [Fact]
    public async Task Send_Query_Throws_When_Query_Is_Invalid()
    {
        var action = () => this._mediator.SendAsync(new SampleQuery(RequiredValue: null!, IsSuccessful: true), CancellationToken.None);
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleQuery), exception.RequestType);
        Assert.Equal("SampleQuery", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this._logs.AssertRequestFailed("Request SampleQuery");
        this._activities.AssertRequestFailed("SampleQuery", exception);
        this._telemetry.AssertRequestFailed("SampleQuery", exception);
    }

    [Fact]
    public async Task Send_Command_Works_When_Handler_Is_Succesful()
    {
        await this._mediator.SendAsync(new SampleCommand("world", IsSuccessful: true), CancellationToken.None);

        this._logs.AssertRequestSuccessful("Request SampleCommand");
        this._activities.AssertRequestSuccessful("SampleCommand");
        this._telemetry.AssertRequestSuccessful("SampleCommand");
    }

    [Fact]
    public async Task Send_Command_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.SendAsync(new SampleCommand("world", IsSuccessful: false), CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        this._logs.AssertRequestFailed("Request SampleCommand");
        this._activities.AssertRequestFailed("SampleCommand", exception);
        this._telemetry.AssertRequestFailed("SampleCommand", exception);
    }

    [Fact]
    public async Task Send_Command_Throws_When_Command_Is_Invalid()
    {
        var action = () => this._mediator.SendAsync(new SampleCommand(RequiredValue: null!, IsSuccessful: true), CancellationToken.None);
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleCommand), exception.RequestType);
        Assert.Equal("SampleCommand", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this._logs.AssertRequestFailed("Request SampleCommand");
        this._activities.AssertRequestFailed("SampleCommand", exception);
        this._telemetry.AssertRequestFailed("SampleCommand", exception);
    }

    [Fact]
    public async Task Send_StreamQuery_Works_When_Handler_Is_Succesful()
    {
        var items = await this._mediator.CreateStream(new SampleStreamQuery("world", IsSuccessful: true), CancellationToken.None).ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal("world!", items[1]);

        this._logs.AssertRequestSuccessful("Stream request SampleStreamQuery");
        this._activities.AssertRequestSuccessful("SampleStreamQuery");
        this._telemetry.AssertRequestSuccessful("SampleStreamQuery");
    }

    [Fact]
    public async Task Send_StreamQuery_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamQuery("world", IsSuccessful: false), CancellationToken.None).ToListAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        this._logs.AssertRequestFailed("Stream request SampleStreamQuery");
        this._activities.AssertRequestFailed("SampleStreamQuery", exception);
        this._telemetry.AssertRequestFailed("SampleStreamQuery", exception);
    }

    [Fact]
    public async Task Send_StreamQuery_Throws_When_StreamQuery_Is_Invalid()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamQuery(RequiredValue: null!, IsSuccessful: true), CancellationToken.None).ToListAsync();
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleStreamQuery), exception.RequestType);
        Assert.Equal("SampleStreamQuery", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this._logs.AssertRequestFailed("Stream request SampleStreamQuery");
        this._activities.AssertRequestFailed("SampleStreamQuery", exception);
        this._telemetry.AssertRequestFailed("SampleStreamQuery", exception);
    }

    [Fact]
    public void Behaviors_Are_Registered_In_The_Right_Order()
    {
        var reverseRequestBehaviors = this.Services.GetServices<IPipelineBehavior<SampleQuery, string>>().Reverse().ToArray();
        var reverseStreamRequestBehaviors = this.Services.GetServices<IStreamPipelineBehavior<SampleStreamQuery, string>>().Reverse().ToArray();

        var expectedRequestCustomBehaviorTypes = new HashSet<Type>
        {
            typeof(RequestTracingBehavior<SampleQuery, string>),
            typeof(RequestApplicationInsightsBehavior<SampleQuery, string>),
            typeof(RequestLoggingBehavior<SampleQuery, string>),
            typeof(RequestValidationBehavior<SampleQuery, string>),
        };

        var expectedCustomStreamRequestBehaviorTypes = new HashSet<Type>
        {
            typeof(StreamRequestTracingBehavior<SampleStreamQuery, string>),
            typeof(StreamRequestApplicationInsightsBehavior<SampleStreamQuery, string>),
            typeof(StreamRequestLoggingBehavior<SampleStreamQuery, string>),
            typeof(StreamRequestValidationBehavior<SampleStreamQuery, string>),
        };

        // OpenTelemetry and ApplicationInsights tracing basically do the same job
        // They must be registered before logging and validation behaviors in order to record logs and validation exceptions
        Assert.IsType<RequestValidationBehavior<SampleQuery, string>>(reverseRequestBehaviors[0]);
        Assert.IsType<RequestLoggingBehavior<SampleQuery, string>>(reverseRequestBehaviors[1]);
        Assert.IsType<RequestApplicationInsightsBehavior<SampleQuery, string>>(reverseRequestBehaviors[2]);
        Assert.IsType<RequestTracingBehavior<SampleQuery, string>>(reverseRequestBehaviors[3]);

        for (var i = 4; i < reverseRequestBehaviors.Length; i++)
        {
            // Any other behavior is not from this library
            Assert.DoesNotContain(reverseRequestBehaviors[i].GetType(), expectedRequestCustomBehaviorTypes);
        }

        Assert.IsType<StreamRequestValidationBehavior<SampleStreamQuery, string>>(reverseStreamRequestBehaviors[0]);
        Assert.IsType<StreamRequestLoggingBehavior<SampleStreamQuery, string>>(reverseStreamRequestBehaviors[1]);
        Assert.IsType<StreamRequestApplicationInsightsBehavior<SampleStreamQuery, string>>(reverseStreamRequestBehaviors[2]);
        Assert.IsType<StreamRequestTracingBehavior<SampleStreamQuery, string>>(reverseStreamRequestBehaviors[3]);

        for (var i = 4; i < reverseStreamRequestBehaviors.Length; i++)
        {
            Assert.DoesNotContain(reverseStreamRequestBehaviors[i].GetType(), expectedCustomStreamRequestBehaviorTypes);
        }
    }
}