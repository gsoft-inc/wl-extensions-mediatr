using Workleap.Extensions.Xunit;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.Extensions.MediatR.Tests;

public sealed class PreAndPostProcessorTests : BaseUnitTest<PreAndPostProcessorTests.Fixture>
{
    public PreAndPostProcessorTests(Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task SendAsync_Executes_Automatically_Registered_Pre_And_Post_Processors()
    {
        var mediator = this.Services.GetRequiredService<IMediator>();
        var state = this.Services.GetRequiredService<TestState>();

        await mediator.SendAsync(new MyCommand(), CancellationToken.None);
        await mediator.SendAsync(new MyQuery(), CancellationToken.None);

        Assert.Equal(1, state.MyHandler_MyCommand_CallCount);
        Assert.Equal(1, state.MyHandler_MyQuery_CallCount);

        Assert.Equal(1, state.MyCommandPreProcessor_Pre_MyCommand_CallCount);
        Assert.Equal(1, state.MyCommandPostProcessor_Post_MyCommand_CallCount);

        Assert.Equal(1, state.MyFirstQueryPreAndPostProcessor_Pre_MyQuery_CallCount);
        Assert.Equal(1, state.MyFirstQueryPreAndPostProcessor_Post_MyQuery_CallCount);

        Assert.Equal(1, state.MySecondQueryPreAndPostProcessor_Pre_MyQuery_CallCount);
        Assert.Equal(1, state.MySecondQueryPreAndPostProcessor_Post_MyQuery_CallCount);
    }

    public sealed class Fixture : BaseUnitFixture
    {
        public override IServiceCollection ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<TestState>();
            services.AddMediator(typeof(MediatorFixture).Assembly);

            // Prevent activities from being emitted and interfere with other tests running in parallel
            // Activity listeners are static and shared across all tests
            for (var i = services.Count - 1; i >= 0; i--)
            {
                if (services[i].ImplementationType == typeof(RequestTracingBehavior<,>) || services[i].ImplementationType == typeof(StreamRequestTracingBehavior<,>))
                {
                    services.RemoveAt(i);
                }
            }

            return services;
        }
    }

    private sealed class TestState
    {
        public int MyHandler_MyCommand_CallCount { get; set; }

        public int MyHandler_MyQuery_CallCount { get; set; }

        public int MyCommandPreProcessor_Pre_MyCommand_CallCount { get; set; }

        public int MyCommandPostProcessor_Post_MyCommand_CallCount { get; set; }

        public int MyFirstQueryPreAndPostProcessor_Pre_MyQuery_CallCount { get; set; }

        public int MyFirstQueryPreAndPostProcessor_Post_MyQuery_CallCount { get; set; }

        public int MySecondQueryPreAndPostProcessor_Pre_MyQuery_CallCount { get; set; }

        public int MySecondQueryPreAndPostProcessor_Post_MyQuery_CallCount { get; set; }
    }

    private sealed record MyCommand : IRequest;

    private sealed record MyQuery : IRequest<bool>;

    private sealed class MyHandler : IRequestHandler<MyCommand>, IRequestHandler<MyQuery, bool>
    {
        private readonly TestState _testState;

        public MyHandler(TestState testState)
        {
            this._testState = testState;
        }

        public Task Handle(MyCommand request, CancellationToken cancellationToken)
        {
            this._testState.MyHandler_MyCommand_CallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> Handle(MyQuery request, CancellationToken cancellationToken)
        {
            this._testState.MyHandler_MyQuery_CallCount++;
            return Task.FromResult(true);
        }
    }

    private sealed class MyCommandPreProcessor : IRequestPreProcessor<MyCommand>
    {
        private readonly TestState _testState;

        public MyCommandPreProcessor(TestState testState)
        {
            this._testState = testState;
        }

        public Task Process(MyCommand request, CancellationToken cancellationToken)
        {
            this._testState.MyCommandPreProcessor_Pre_MyCommand_CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class MyCommandPostProcessor : IRequestPostProcessor<MyCommand, Unit>
    {
        private readonly TestState _testState;

        public MyCommandPostProcessor(TestState testState)
        {
            this._testState = testState;
        }

        public Task Process(MyCommand request, Unit response, CancellationToken cancellationToken)
        {
            this._testState.MyCommandPostProcessor_Post_MyCommand_CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class MyFirstQueryPreAndPostProcessor : IRequestPreProcessor<MyQuery>, IRequestPostProcessor<MyQuery, bool>
    {
        private readonly TestState _testState;

        public MyFirstQueryPreAndPostProcessor(TestState testState)
        {
            this._testState = testState;
        }

        public Task Process(MyQuery request, CancellationToken cancellationToken)
        {
            this._testState.MyFirstQueryPreAndPostProcessor_Pre_MyQuery_CallCount++;
            return Task.CompletedTask;
        }

        public Task Process(MyQuery request, bool response, CancellationToken cancellationToken)
        {
            this._testState.MyFirstQueryPreAndPostProcessor_Post_MyQuery_CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class MySecondQueryPreAndPostProcessor : IRequestPreProcessor<MyQuery>, IRequestPostProcessor<MyQuery, bool>
    {
        private readonly TestState _testState;

        public MySecondQueryPreAndPostProcessor(TestState testState)
        {
            this._testState = testState;
        }

        public Task Process(MyQuery request, CancellationToken cancellationToken)
        {
            this._testState.MySecondQueryPreAndPostProcessor_Pre_MyQuery_CallCount++;
            return Task.CompletedTask;
        }

        public Task Process(MyQuery request, bool response, CancellationToken cancellationToken)
        {
            this._testState.MySecondQueryPreAndPostProcessor_Post_MyQuery_CallCount++;
            return Task.CompletedTask;
        }
    }
}