using System.Diagnostics;
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

    public MediatorTests(MediatorFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._mediator = this.Services.GetRequiredService<IMediator>();
        this._logs = this.Services.GetRequiredService<InMemoryLoggerTracker>();
        this._activities = this.Services.GetRequiredService<InMemoryActivityTracker>();
    }

    [Fact]
    public void Behaviors_Are_Registered_In_The_Right_Order()
    {
        const int customRegisteredBehaviorCount = 4;

        // Only take the behaviors we register, the first others are added by MediatR itself
        // If we ever add more custom behaviors, increment that constant
        var behaviors = this.Services.GetServices<IPipelineBehavior<SampleRequest, string>>().TakeLast(customRegisteredBehaviorCount).ToArray();
        var streamBehaviors = this.Services.GetServices<IStreamPipelineBehavior<SampleStreamRequest, string>>().TakeLast(customRegisteredBehaviorCount).ToArray();

        Assert.Equal(customRegisteredBehaviorCount, behaviors.Length);
        Assert.Equal(customRegisteredBehaviorCount, streamBehaviors.Length);

        // ApplicationInsights and OpenTelemetry tracing basically do the same job
        // They must be registered before logging and validation behaviors in order to record logs and validation exceptions
        Assert.IsType<RequestApplicationInsightsBehavior<SampleRequest, string>>(behaviors[0]);
        Assert.IsType<RequestTracingBehavior<SampleRequest, string>>(behaviors[1]);
        Assert.IsType<RequestLoggingBehavior<SampleRequest, string>>(behaviors[2]);
        Assert.IsType<RequestValidationBehavior<SampleRequest, string>>(behaviors[3]);

        Assert.IsType<StreamRequestApplicationInsightsBehavior<SampleStreamRequest, string>>(streamBehaviors[0]);
        Assert.IsType<StreamRequestTracingBehavior<SampleStreamRequest, string>>(streamBehaviors[1]);
        Assert.IsType<StreamRequestLoggingBehavior<SampleStreamRequest, string>>(streamBehaviors[2]);
        Assert.IsType<StreamRequestValidationBehavior<SampleStreamRequest, string>>(streamBehaviors[3]);
    }

    [Fact]
    public async Task Send_Request_Works_When_Handler_Is_Succesful()
    {
        var result = await this._mediator.Send(new SampleRequest("world", IsSuccessful: true));
        Assert.Equal("Hello world!", result);

        this.AssertRequestSuccessfulLogs("Request SampleRequest");
        this.AssertRequestSuccessfulActivity();
    }

    [Fact]
    public async Task Send_Request_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.Send(new SampleRequest("world", IsSuccessful: false));
        await Assert.ThrowsAsync<InvalidOperationException>(action);

        this.AssertRequestHandlerFailedActivity();
        this.AssertRequestFailedLogs("Request SampleRequest");
    }

    [Fact]
    public async Task Send_Request_Throws_When_Request_Is_Invalid()
    {
        var action = () => this._mediator.Send(new SampleRequest(RequiredValue: null!, IsSuccessful: true));
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleRequest), exception.RequestType);
        Assert.Equal("SampleRequest", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this.AssertRequestValidationFailedActivity();
        this.AssertRequestFailedLogs("Request SampleRequest");
    }

    [Fact]
    public async Task Send_StreamRequest_Works_When_Handler_Is_Succesful()
    {
        var items = await this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: true)).ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal("world!", items[1]);

        this.AssertRequestSuccessfulLogs("Stream request SampleStreamRequest");
        this.AssertRequestSuccessfulActivity();
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_Handler_Fails()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: false)).ToListAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(action);

        this.AssertRequestHandlerFailedActivity();
        this.AssertRequestFailedLogs("Stream request SampleStreamRequest");
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_StreamRequest_Is_Invalid()
    {
        var action = () => this._mediator.CreateStream(new SampleStreamRequest(RequiredValue: null!, IsSuccessful: true)).ToListAsync();
        var exception = await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Equal(typeof(SampleStreamRequest), exception.RequestType);
        Assert.Equal("SampleStreamRequest", exception.RequestName);
        Assert.Contains("RequiredValue", exception.Message);

        this.AssertRequestValidationFailedActivity();
        this.AssertRequestFailedLogs("Stream request SampleStreamRequest");
    }

    private void AssertRequestSuccessfulLogs(string prefix)
    {
        Assert.Equal(2, this._logs.Count);
        Assert.Single(this._logs, x => x.StartsWith(prefix + " started"));
        Assert.Single(this._logs, x => x.StartsWith(prefix + " ended successfully"));
    }

    private void AssertRequestFailedLogs(string prefix)
    {
        Assert.Equal(2, this._logs.Count);
        Assert.Single(this._logs, x => x.StartsWith(prefix + " started"));
        Assert.Single(this._logs, x => x.StartsWith(prefix + " failed after"));
    }

    private void AssertRequestSuccessfulActivity()
    {
        var activity = Assert.Single(this._activities);

        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("OK", activity.GetTagItem(TracingHelper.StatusCodeTag));
    }

    private void AssertRequestHandlerFailedActivity()
    {
        var activity = Assert.Single(this._activities);

        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        Assert.Equal("ERROR", activity.GetTagItem(TracingHelper.StatusCodeTag));
        Assert.Equal("Something wrong happened", activity.GetTagItem(TracingHelper.StatusDescriptionTag));
        Assert.Equal("Something wrong happened", activity.GetTagItem(TracingHelper.ExceptionMessageTag));
        Assert.Equal("System.InvalidOperationException", activity.GetTagItem(TracingHelper.ExceptionTypeTag));

        var stacktrace = Assert.IsType<string>(activity.GetTagItem(TracingHelper.ExceptionStackTraceTag));
        Assert.NotEmpty(stacktrace);
    }

    private void AssertRequestValidationFailedActivity()
    {
        var activity = Assert.Single(this._activities);

        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        Assert.Equal("ERROR", activity.GetTagItem(TracingHelper.StatusCodeTag));

        var statusDescription = Assert.IsType<string>(activity.GetTagItem(TracingHelper.StatusDescriptionTag));
        Assert.Contains("Validation failed for", statusDescription);

        var exceptionMessage = Assert.IsType<string>(activity.GetTagItem(TracingHelper.ExceptionMessageTag));
        Assert.Contains("Validation failed for", exceptionMessage);

        Assert.Equal("GSoft.Extensions.MediatR.RequestValidationException", activity.GetTagItem(TracingHelper.ExceptionTypeTag));

        var stacktrace = Assert.IsType<string>(activity.GetTagItem(TracingHelper.ExceptionStackTraceTag));
        Assert.NotEmpty(stacktrace);
    }
}