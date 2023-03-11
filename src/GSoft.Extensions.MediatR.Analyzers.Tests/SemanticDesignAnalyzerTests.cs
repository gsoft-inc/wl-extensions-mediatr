namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class SemanticDesignAnalyzerTests : BaseAnalyzerTest<SemanticDesignAnalyzer>
{
    [Fact]
    public async Task Query_Handlers_Cant_Call_Query_Or_Command_Handlers()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyOtherCommand : IRequest { }

public class MyOtherQuery : IRequest<string> { }

internal class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    public async Task<string> Handle(MyQuery query, CancellationToken cancellationToken)
    {
        var mediator = (IMediator)null!;
        await mediator.Send(new MyOtherCommand(), CancellationToken.None);
        await mediator.Send(new MyOtherQuery(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherCommand(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherQuery(), CancellationToken.None);
        return string.Empty;
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 13, startColumn: 24, endLine: 13, endColumn: 28)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 14, startColumn: 24, endLine: 14, endColumn: 28)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 15, startColumn: 24, endLine: 15, endColumn: 33)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 16, startColumn: 24, endLine: 16, endColumn: 33)
            .RunAsync();
    }

    [Fact]
    public async Task Command_Handlers_Can_Call_Query_Handler_But_Not_Command_Handlers()
    {
        const string source = @"
public class MyCommand : IRequest { }

public class MyOtherCommand : IRequest { }

public class MyOtherQuery : IRequest<string> { }

internal class MyCommandHandler : IRequestHandler<MyCommand>
{
    public async Task Handle(MyCommand command, CancellationToken cancellationToken)
    {
        var mediator = (IMediator)null!;
        await mediator.Send(new MyOtherCommand(), CancellationToken.None);
        await mediator.Send(new MyOtherQuery(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherCommand(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherQuery(), CancellationToken.None);
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 13, startColumn: 24, endLine: 13, endColumn: 28)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, startLine: 15, startColumn: 24, endLine: 15, endColumn: 33)
            .RunAsync();
    }

    [Fact]
    public async Task Call_Mediator_Publish_Method_In_Query_Handler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyNotification : INotification { }

internal class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    public async Task<string> Handle(MyQuery query, CancellationToken cancellationToken)
    {
        var mediator = (IMediator)null!;
        await mediator.Publish(new MyNotification(), CancellationToken.None);
        await mediator.PublishAsync(new MyNotification(), CancellationToken.None);
        return string.Empty;
    }
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Call_Mediator_Publish_Method_In_Command_Handler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyCommand : IRequest { }

public class MyNotification : INotification { }

internal class MyCommandHandler : IRequestHandler<MyCommand>
{
    public async Task Handle(MyCommand command, CancellationToken cancellationToken)
    {
        var mediator = (IMediator)null!;
        await mediator.Publish(new MyNotification(), CancellationToken.None);
        await mediator.PublishAsync(new MyNotification(), CancellationToken.None);
    }
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Handlers_Cannot_Be_Public()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

public class MyCommand : IRequest { }

public class MyNotification : INotification { }

public class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    public Task<string> Handle(MyQuery query, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
}

public class MyCommandHandler : IRequestHandler<MyCommand>
{
    public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class MyNotificationHandler : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, startLine: 8, startColumn: 14, endLine: 8, endColumn: 28)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, startLine: 13, startColumn: 14, endLine: 13, endColumn: 30)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, startLine: 18, startColumn: 14, endLine: 18, endColumn: 35)
            .RunAsync();
    }

    [Fact]
    public async Task Internal_NotificationHandler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyNotification : INotification { }

internal class MyNotificationHandler : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source).RunAsync();
    }
}