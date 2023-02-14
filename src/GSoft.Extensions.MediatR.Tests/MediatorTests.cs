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
        // TODO resolve behaviors and validate their order
    }
}