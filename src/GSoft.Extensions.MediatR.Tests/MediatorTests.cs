using System.Diagnostics.CodeAnalysis;
using GSoft.Extensions.Xunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.Extensions.MediatR.Tests;

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
    public async Task Send_Request_Works_When_Handler_Is_Succesful()
    {
        var result = await this._mediator.Send(new SampleRequest("world", IsSuccessful: true));
        Assert.Equal("Hello world!", result);

        this._logs.AssertRequestSuccessful("Request SampleRequest");
        this._activities.AssertRequestSuccessful("SampleRequest");
        this._telemetry.AssertRequestSuccessful("SampleRequest");
    }

    [Fact]
    public async Task Send_Request_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.Send(new SampleRequest("world", IsSuccessful: false));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        this._logs.AssertRequestFailed("Request SampleRequest");
        this._activities.AssertRequestFailed("SampleRequest", exception);
        this._telemetry.AssertRequestFailed("SampleRequest", exception);
    }

    [Fact]
    public async Task Send_Request_Throws_When_Request_Is_Invalid()
    {
        var action = () => this._mediator.Send(new SampleRequest(RequiredValue: null!, IsSuccessful: true));
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleRequest), exception.RequestType);
        Assert.Equal("SampleRequest", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this._logs.AssertRequestFailed("Request SampleRequest");
        this._activities.AssertRequestFailed("SampleRequest", exception);
        this._telemetry.AssertRequestFailed("SampleRequest", exception);
    }

    [Fact]
    public async Task Send_StreamRequest_Works_When_Handler_Is_Succesful()
    {
        var items = await this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: true)).ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal("world!", items[1]);

        this._logs.AssertRequestSuccessful("Stream request SampleStreamRequest");
        this._activities.AssertRequestSuccessful("SampleStreamRequest");
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: false)).ToListAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

        this._logs.AssertRequestFailed("Stream request SampleStreamRequest");
        this._activities.AssertRequestFailed("SampleStreamRequest", exception);
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_StreamRequest_Is_Invalid()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamRequest(RequiredValue: null!, IsSuccessful: true)).ToListAsync();
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleStreamRequest), exception.RequestType);
        Assert.Equal("SampleStreamRequest", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this._logs.AssertRequestFailed("Stream request SampleStreamRequest");
        this._activities.AssertRequestFailed("SampleStreamRequest", exception);
    }

    [Fact]
    public void Behaviors_Are_Registered_In_The_Right_Order()
    {
        // Only take the behaviors we register, the first others are added by MediatR itself
        // If we ever add more custom behaviors, increment that constant
        var reverseRequestBehaviors = this.Services.GetServices<IPipelineBehavior<SampleRequest, string>>().Reverse().ToArray();
        var reverseStreamRequestBehaviors = this.Services.GetServices<IStreamPipelineBehavior<SampleStreamRequest, string>>().Reverse().ToArray();

        var expectedRequestCustomBehaviorTypes = new HashSet<Type>
        {
            typeof(RequestTracingBehavior<SampleRequest, string>),
            typeof(RequestApplicationInsightsBehavior<SampleRequest, string>),
            typeof(RequestLoggingBehavior<SampleRequest, string>),
            typeof(RequestValidationBehavior<SampleRequest, string>),
        };

        var expectedCustomStreamRequestBehaviorTypes = new HashSet<Type>
        {
            typeof(StreamRequestTracingBehavior<SampleStreamRequest, string>),
            typeof(StreamRequestApplicationInsightsBehavior<SampleStreamRequest, string>),
            typeof(StreamRequestLoggingBehavior<SampleStreamRequest, string>),
            typeof(StreamRequestValidationBehavior<SampleStreamRequest, string>),
        };

        // OpenTelemetry and ApplicationInsights tracing basically do the same job
        // They must be registered before logging and validation behaviors in order to record logs and validation exceptions
        Assert.IsType<RequestValidationBehavior<SampleRequest, string>>(reverseRequestBehaviors[0]);
        Assert.IsType<RequestLoggingBehavior<SampleRequest, string>>(reverseRequestBehaviors[1]);
        Assert.IsType<RequestApplicationInsightsBehavior<SampleRequest, string>>(reverseRequestBehaviors[2]);
        Assert.IsType<RequestTracingBehavior<SampleRequest, string>>(reverseRequestBehaviors[3]);

        for (var i = 4; i < reverseRequestBehaviors.Length; i++)
        {
            // Any other behavior is not from this library
            Assert.DoesNotContain(reverseRequestBehaviors[i].GetType(), expectedRequestCustomBehaviorTypes);
        }

        Assert.IsType<StreamRequestValidationBehavior<SampleStreamRequest, string>>(reverseStreamRequestBehaviors[0]);
        Assert.IsType<StreamRequestLoggingBehavior<SampleStreamRequest, string>>(reverseStreamRequestBehaviors[1]);
        Assert.IsType<StreamRequestApplicationInsightsBehavior<SampleStreamRequest, string>>(reverseStreamRequestBehaviors[2]);
        Assert.IsType<StreamRequestTracingBehavior<SampleStreamRequest, string>>(reverseStreamRequestBehaviors[3]);

        for (var i = 4; i < reverseStreamRequestBehaviors.Length; i++)
        {
            Assert.DoesNotContain(reverseStreamRequestBehaviors[i].GetType(), expectedCustomStreamRequestBehaviorTypes);
        }
    }
}