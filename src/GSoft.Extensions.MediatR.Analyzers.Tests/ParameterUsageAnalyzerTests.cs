using MediatR;

namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class ParameterUsageAnalyzerTests : BaseAnalyzerTest<ParameterUsageAnalyzer>
{
    private const string ProgramSourceCodeFormat = @"
public class MyQuery : IRequest<string> {{ }}

public class MyStreamQuery : IStreamRequest<string> {{ }}

public class MyNotification : INotification {{ }}

public class MyWorker
{{
    public void DoSomething()
    {{
        var mediator = ({0})null!;
        {1}
    }}
}}";

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_Send_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Send(new MyQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.CreateStream(new MyStreamQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Publish(new MyNotification(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_Send_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Send((object)new MyQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 26)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.CreateStream((object)new MyStreamQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 34)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Non_Generic_Publish_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Publish((object)new MyNotification(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 29)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_Send_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Send(new MyQuery());";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 26)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.CreateStream(new MyStreamQuery());";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 34)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Publish(new MyNotification());";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithDisabledDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule)
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 29)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task SendAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.SendAsync(new MyQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source)).RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task PublishAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.PublishAsync(new MyNotification(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source)).RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Send_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Send(new MyQuery(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 26)
            .RunAsync();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Publish_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        const string source = "_ = mediator.Publish(new MyNotification(), CancellationToken.None);";
        await this.WithSourceCode(string.Format(ProgramSourceCodeFormat, methodContainingType, source))
            .WithExpectedDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule, startLine: 13, startColumn: 22, endLine: 13, endColumn: 29)
            .RunAsync();
    }
}