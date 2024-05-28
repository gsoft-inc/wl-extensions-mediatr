namespace Workleap.Extensions.MediatR.Analyzers.Tests;

public sealed class SemanticDesignAnalyzerTests : BaseAnalyzerTest<SemanticDesignAnalyzer>
{
    [Fact]
    public async Task Query_Handlers_Cant_Send_Query_Or_Command()
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
        await mediator.{|#0:Send|}(new MyOtherCommand(), CancellationToken.None);
        await mediator.{|#1:Send|}(new MyOtherQuery(), CancellationToken.None);
        await mediator.{|#2:SendAsync|}(new MyOtherCommand(), CancellationToken.None);
        await mediator.{|#3:SendAsync|}(new MyOtherQuery(), CancellationToken.None);
        return string.Empty;
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 0)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 1)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 2)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 3)
            .RunAsync();
    }

    [Fact]
    public async Task Command_Handlers_Can_Send_Query_But_Not_Command()
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
        await mediator.{|#0:Send|}(new MyOtherCommand(), CancellationToken.None);
        await mediator.Send(new MyOtherQuery(), CancellationToken.None);
        await mediator.{|#1:SendAsync|}(new MyOtherCommand(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherQuery(), CancellationToken.None);
    }
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 0)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotCallHandlerRule, 1)
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

public class {|#0:MyQueryHandler|} : IRequestHandler<MyQuery, string>
{
    public Task<string> Handle(MyQuery query, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
}

public class {|#1:MyCommandHandler|} : IRequestHandler<MyCommand>
{
    public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class {|#2:MyNotificationHandler|} : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, 0)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, 1)
            .WithExpectedDiagnostic(SemanticDesignAnalyzer.HandlersShouldNotBePublicRule, 2)
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

    [Fact]
    public async Task Notification_Handlers_Can_Send_Query_Or_Command()
    {
        const string source = @"
public class MyOtherQuery : IRequest<string> { }

public class MyOtherCommand : IRequest { }

public class MyNotification : INotification { }

internal class MyNotificationHandler : INotificationHandler<MyNotification>
{
    public async Task Handle(MyNotification notification, CancellationToken cancellationToken)
    {
        var mediator = (IMediator)null!;
        await mediator.Send(new MyOtherCommand(), CancellationToken.None);
        await mediator.Send(new MyOtherQuery(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherCommand(), CancellationToken.None);
        await mediator.SendAsync(new MyOtherQuery(), CancellationToken.None);
        return;
    }
}";

        await this.WithSourceCode(source).RunAsync();
    }
}