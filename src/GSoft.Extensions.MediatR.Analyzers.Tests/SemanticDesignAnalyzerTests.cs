namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class SemanticDesignAnalyzerTests : BaseAnalyzerTests<SemanticDesignAnalyzer>
{
    [Fact]
    public async Task Call_Mediator_Send_Method_In_Handler_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyOtherCommand : IRequest { }

public class MyOtherQuery : IRequest<string> { }

internal class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    private readonly IMediator _mediator;

    public MyQueryHandler(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public async Task<string> Handle(MyQuery query, CancellationToken cancellationToken)
    {
        await this._mediator.Send(new MyOtherCommand());
        await this._mediator.Send(new MyOtherQuery());
        return string.Empty;
    }
}";

        var diagnostics = await this.Helper.WithSourceFile("Program.cs", source).Compile();
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, x => Assert.Same(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, x.Descriptor));
    }

    [Fact]
    public async Task Call_Mediator_Publish_Method_In_Handler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyNotification : INotification { }

internal class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    private readonly IMediator _mediator;

    public MyQueryHandler(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public async Task<string> Handle(MyQuery query, CancellationToken cancellationToken)
    {
        await this._mediator.Publish(new MyNotification());
        return string.Empty;
    }
}";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", source).Compile();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Public_RequestHandler_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    public Task<string> Handle(MyQuery query, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
}";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", source).Compile();
        Assert.Same(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, Assert.Single(diagnostics).Descriptor);
    }
}