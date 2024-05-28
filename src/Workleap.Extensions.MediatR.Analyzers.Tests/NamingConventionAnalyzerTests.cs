namespace Workleap.Extensions.MediatR.Analyzers.Tests;

public sealed class NamingConventionAnalyzerTests : BaseAnalyzerTest<NamingConventionAnalyzer>
{
    [Fact]
    public async Task Request_Ending_With_Command_Returns_No_Diagnostic()
    {
        const string source = "public class MyCommand : IRequest { }";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Request_Ending_With_Query_Returns_No_Diagnostic()
    {
        const string source = "public class MyQuery : IRequest<string> { }";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task StreamRequest_Ending_With_StreamQuery_Returns_No_Diagnostic()
    {
        const string source = "public class MyStreamQuery : IStreamRequest<string> { }";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Notification_Ending_With_Notification_Returns_No_Diagnostic()
    {
        const string source = "public class MyNotification : INotification { }";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Notification_Ending_With_Event_Returns_No_Diagnostic()
    {
        const string source = "public class MyEvent : INotification { }";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task Request_Not_Ending_With_Command_Or_Query_Returns_One_Diagnostic()
    {
        const string source = "public sealed class {|#0:MyClass|} : IRequest<string> { }";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseCommandOrQuerySuffixRule, 0)
            .RunAsync();
    }

    [Fact]
    public async Task StreamRequest_Not_Ending_With_StreamQuery_Returns_One_Diagnostic()
    {
        const string source = "public class {|#0:MyClass|} : IStreamRequest<string> { }";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseStreamQuerySuffixRule, 0)
            .RunAsync();
    }

    [Fact]
    public async Task Notification_Not_Ending_With_Notification_Or_Event_Returns_One_Diagnostic()
    {
        const string source = "public class {|#0:MyClass|} : INotification { }";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseNotificationOrEventSuffixRule, 0)
            .RunAsync();
    }

    [Fact]
    public async Task RequestHandler_Ending_With_CommandHandler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyCommand : IRequest { }

internal class MyCommandHandler : IRequestHandler<MyCommand>
{
    public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Fact]
    public async Task RequestHandler_Ending_With_QueryHandler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyQuery : IRequest<string> { }

internal class MyQueryHandler : IRequestHandler<MyQuery, string>
{
    public Task<string> Handle(MyQuery query, CancellationToken cancellationToken) => Task.FromResult(string.Empty);
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Theory]
    [InlineData("Handler")]
    [InlineData("MyCommandHandler")]
    public async Task Nested_RequestHandler_Ending_With_Handler_Returns_No_Diagnostic(string handlerName)
    {
        const string source = @"
public sealed record MyCommand : IRequest
{{
    internal sealed class {0} : IRequestHandler<MyCommand>
    {{
        public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerName)).RunAsync();
    }

    [Fact]
    public async Task StreamRequestHandler_Ending_With_StreamQueryHandler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyStreamQuery : IStreamRequest<string> { }

internal class MyStreamQueryHandler : IStreamRequestHandler<MyStreamQuery, string>
{
    public async IAsyncEnumerable<string> Handle(MyStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield break;
    }
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Theory]
    [InlineData("Handler")]
    [InlineData("MyStreamQueryHandler")]
    public async Task Nested_StreamRequestHandler_Ending_With_Handler_Returns_No_Diagnostic(string handlerName)
    {
        const string source = @"
public class MyStreamQuery : IStreamRequest<string>
{{
    internal class {0} : IStreamRequestHandler<MyStreamQuery, string>
    {{
        public async IAsyncEnumerable<string> Handle(MyStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {{
            yield break;
        }}
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerName)).RunAsync();
    }

    [Fact]
    public async Task NotificationHandler_Ending_With_NotificationHandler_Returns_No_Diagnostic()
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
    public async Task NotificationHandler_Ending_With_EventHandler_Returns_No_Diagnostic()
    {
        const string source = @"
public class MyEvent : INotification { }

internal class MyEventHandler : INotificationHandler<MyEvent>
{
    public Task Handle(MyEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source).RunAsync();
    }

    [Theory]
    [InlineData("Handler")]
    [InlineData("MynotificationHandler")]
    public async Task Nested_NotificationHandler_Ending_With_Handler_Returns_No_Diagnostic(string handlerName)
    {
        const string source = @"
public class MyNotification : INotification
{{
    internal class {0} : INotificationHandler<MyNotification>
    {{
        public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerName)).RunAsync();
    }

    [Fact]
    public async Task RequestHandler_Not_Ending_With_CommandHandler_Or_QueryHandler_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyCommand : IRequest { }

internal class {|#0:MyRequestHandler|} : IRequestHandler<MyCommand>
{
    public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseCommandHandlerOrQueryHandlerSuffixRule, 0)
            .RunAsync();
    }

    [Fact]
    public async Task StreamRequestHandler_Not_Ending_With_StreamQueryHandler_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyStreamQuery : IStreamRequest<string> { }

internal class {|#0:MyStreamRequestHandler|} : IStreamRequestHandler<MyStreamQuery, string>
{
    public async IAsyncEnumerable<string> Handle(MyStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield break;
    }
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseStreamQueryHandlerSuffixRule, 0)
            .RunAsync();
    }

    [Fact]
    public async Task NotificationHandler_Not_Ending_With_NotificationHandler_Or_EventHandler_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyNotification : INotification { }

internal class {|#0:SomethingHandler|} : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseNotificationHandlerOrEventHandlerSuffixRule, 0)
            .RunAsync();
    }

    [Theory]
    [InlineData("HandlerClass")]
    [InlineData("HandlingClass")]
    public async Task Nested_RequestHandler_Not_Ending_With_Handler_Returns_One_Diagnostic(string handlerClassName)
    {
        const string source = @"
public sealed record MyCommand : IRequest
{{
    internal sealed class {{|#0:{0}|}} : IRequestHandler<MyCommand>
    {{
        public Task Handle(MyCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerClassName))
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseHandlerSuffixRule, 0)
            .RunAsync();
    }

    [Theory]
    [InlineData("HandlerClass")]
    [InlineData("HandlingClass")]
    public async Task Nested_StreamRequestHandler_Not_Ending_With_Handler_Returns_One_Diagnostic(string handlerClassName)
    {
        const string source = @"
public class MyStreamQuery : IStreamRequest<string>
{{
    internal class {{|#0:{0}|}} : IStreamRequestHandler<MyStreamQuery, string>
    {{
        public async IAsyncEnumerable<string> Handle(MyStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {{
            yield break;
        }}
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerClassName))
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseHandlerSuffixRule, 0)
            .RunAsync();
    }

    [Theory]
    [InlineData("HandlerClass")]
    [InlineData("HandlingClass")]
    public async Task Nested_NotificationHandler_Not_Ending_With_Handler_Returns_One_Diagnostic(string handlerClassName)
    {
        const string source = @"
public class MyNotification : INotification
{{
    internal class {{|#0:{0}|}} : INotificationHandler<MyNotification>
    {{
        public Task Handle(MyNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }}
}}";
        await this.WithSourceCode(string.Format(source, handlerClassName))
            .WithExpectedDiagnostic(NamingConventionAnalyzer.UseHandlerSuffixRule, 0)
            .RunAsync();
    }
}