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
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream(new MyStreamQuery(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Explicit_CancellationToken_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_Send_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send((object)new MyQuery(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.UseGenericParameterRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Non_Generic_CreateStream_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream((object)new MyStreamQuery(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.UseGenericParameterRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Non_Generic_Publish_Method_With_Explicit_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish((object)new MyNotification(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.UseGenericParameterRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_Send_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send(new MyQuery());";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.ProvideCancellationTokenRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Generic_CreateStream_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).CreateStream(new MyStreamQuery());";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.ProvideCancellationTokenRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Generic_Publish_Method_With_Default_CancellationToken_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification());";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormatWithoutAsyncMethodRule, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.ProvideCancellationTokenRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task SendAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).SendAsync(new MyQuery(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormat, source)).Compile();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task PublishAsync_Method_Returns_No_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).PublishAsync(new MyNotification(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormat, source)).Compile();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(ISender))]
    public async Task Send_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Send(new MyQuery(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormat, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule, Assert.Single(diagnostics).Descriptor);
    }

    [Theory]
    [InlineData(nameof(Mediator))]
    [InlineData(nameof(IMediator))]
    [InlineData(nameof(IPublisher))]
    public async Task Publish_Without_Async_Extension_Method_Returns_One_Diagnostic(string methodContainingType)
    {
        var source = $"_ = (({methodContainingType})null!).Publish(new MyNotification(), CancellationToken.None);";
        var diagnostics = await this.Helper.WithSourceFile("Program.cs", string.Format(ProgramSourceCodeFormat, source)).Compile();
        Assert.Same(ParameterUsageAnalyzer.UseMethodEndingWithAsyncRule, Assert.Single(diagnostics).Descriptor);
    }
}