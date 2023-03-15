namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class SemanticDesignAnalyzerTests : BaseAnalyzerTest<SemanticDesignAnalyzer>
{
    [Fact]
    public async Task Call_Mediator_Send_Or_SendAsync_Method_In_Handler_Returns_Four_Diagnostics()
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
        await this._mediator.Send(new MyOtherCommand(), CancellationToken.None);
        await this._mediator.Send(new MyOtherQuery(), CancellationToken.None);
        await this._mediator.SendAsync(new MyOtherCommand(), CancellationToken.None);
        await this._mediator.SendAsync(new MyOtherQuery(), CancellationToken.None);
        return string.Empty;
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 19, startColumn: 30, endLine: 19, endColumn: 34)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 20, startColumn: 30, endLine: 20, endColumn: 34)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 21, startColumn: 30, endLine: 21, endColumn: 39)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 22, startColumn: 30, endLine: 22, endColumn: 39)
            .RunAsync();
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
        await this._mediator.Publish(new MyNotification(), CancellationToken.None);
        await this._mediator.PublishAsync(new MyNotification(), CancellationToken.None);
        return string.Empty;
    }
}";
        await this.WithSourceCode(source).RunAsync();
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
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, startLine: 4, startColumn: 14, endLine: 4, endColumn: 28)
            .RunAsync();
    }
}