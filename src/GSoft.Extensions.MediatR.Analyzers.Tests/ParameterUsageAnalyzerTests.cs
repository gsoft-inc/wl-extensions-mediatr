using MediatR;

namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class ParameterUsageAnalyzerTests : BaseAnalyzerTests<ParameterUsageAnalyzer>
{
    private const string ProgramSourceCodeFormat = @"
public class MyQuery : IRequest<string> {{ }}

public class MyStreamQuery : IStreamRequest<string> {{ }}

public class MyNotification : INotification {{ }}

public class MyWorker
{{
    public void DoSomething()
    {{
        {0}
    }}
}}";

    private const string ProgramSourceCodeFormatWithoutAsyncMethodRule =
        "#pragma warning disable GMDTR12: this warning is tested in isolation\r\n" + ProgramSourceCodeFormat;

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_Send_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send(new MyQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithoutDiagnostics();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream(new MyStreamQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithoutDiagnostics();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithoutDiagnostics();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_Send_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send((object)new MyQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream((object)new MyStreamQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Non_Generic_Publish_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish((object)new MyNotification(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.UseGenericParameterRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_Send_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send(new MyQuery());";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream(new MyStreamQuery());";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification());";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.ProvideCancellationTokenRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task SendAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).SendAsync(new MyQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormat, source)).ShouldCompileWithoutDiagnostics();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task PublishAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).PublishAsync(new MyNotification(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormat, source))
            .ShouldCompileWithoutDiagnostics();
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Send_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send(new MyQuery(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormat, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Publish_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification(), CancellationToken.None);";
        await this.Builder.WithSourceCode(string.Format(ProgramSourceCodeFormat, source))
            .ShouldCompileWithDiagnostic(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule);
    }
}