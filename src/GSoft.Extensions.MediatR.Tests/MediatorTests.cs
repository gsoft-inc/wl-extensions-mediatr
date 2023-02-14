using GSoft.Extensions.Xunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.Extensions.MediatR.Tests;

public sealed class MediatorTests : BaseUnitTest<MediatorFixture>
{
    private readonly IMediator _mediator;

    public MediatorTests(MediatorFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        this._mediator = this.Services.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_Request_Works_When_Handler_Is_Succesful()
    {
        var result = await this._mediator.Send(new SampleRequest("world", IsSuccessful: true));
        Assert.Equal("Hello world!", result);
    }

    [Fact]
    public async Task Send_Request_Throws_When_Handler_Fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => this._mediator.Send(new SampleRequest("world", IsSuccessful: false)));
    }

    [Fact]
    public async Task Send_Request_Throws_When_Request_Is_Invalid()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => this._mediator.Send(new SampleRequest(RequiredValue: null!, IsSuccessful: true)));
    }

    [Fact]
    public async Task Send_StreamRequest_Works_When_Handler_Is_Succesful()
    {
        var items = await this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: true)).ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Equal("Hello", items[0]);
        Assert.Equal("world!", items[1]);
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_Handler_Fails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => this._mediator.CreateStream(new SampleStreamRequest("world", IsSuccessful: false)).ToListAsync());
    }

    [Fact]
    public async Task Send_StreamRequest_Throws_When_StreamRequest_Is_Invalid()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => this._mediator.CreateStream(new SampleStreamRequest(RequiredValue: null!, IsSuccessful: true)).ToListAsync());
    }

    [Fact]
    public void Services_Are_Registered_In_The_Right_Order()
    {
        const int customRegisteredBehaviorCount = 4;

        // Only take the behaviors we register, the first others are added by MediatR itself
        // If we ever add more custom behaviors, increment that constant
        var behaviors = this.Services.GetServices<IPipelineBehavior<SampleRequest, string>>().TakeLast(customRegisteredBehaviorCount).ToArray();
        var streamBehaviors = this.Services.GetServices<IStreamPipelineBehavior<SampleStreamRequest, string>>().TakeLast(customRegisteredBehaviorCount).ToArray();

        Assert.Equal(customRegisteredBehaviorCount, behaviors.Length);
        Assert.Equal(customRegisteredBehaviorCount, streamBehaviors.Length);

        // Application Insights and OpenTelemetry tracing basically do the same job
        // They must be registered before logging and validation behaviors in order to record their logs and exceptions
        Assert.IsType<RequestApplicationInsightsBehavior<SampleRequest, string>>(behaviors[0]);
        Assert.IsType<RequestTracingBehavior<SampleRequest, string>>(behaviors[1]);
        Assert.IsType<RequestLoggingBehavior<SampleRequest, string>>(behaviors[2]);
        Assert.IsType<RequestValidationBehavior<SampleRequest, string>>(behaviors[3]);

        Assert.IsType<StreamRequestApplicationInsightsBehavior<SampleStreamRequest, string>>(streamBehaviors[0]);
        Assert.IsType<StreamRequestTracingBehavior<SampleStreamRequest, string>>(streamBehaviors[1]);
        Assert.IsType<StreamRequestLoggingBehavior<SampleStreamRequest, string>>(streamBehaviors[2]);
        Assert.IsType<StreamRequestValidationBehavior<SampleStreamRequest, string>>(streamBehaviors[3]);
    }
}