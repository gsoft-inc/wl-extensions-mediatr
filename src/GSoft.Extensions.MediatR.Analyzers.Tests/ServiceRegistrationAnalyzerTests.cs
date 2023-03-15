namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class ServiceRegistrationAnalyzerTests : BaseAnalyzerTest<ServiceRegistrationAnalyzer>
{
    [Fact]
    public async Task Forbidden_AddMediatR_Method_Returns_One_Diagnostic()
    {
        const string source = @"
public class MyRegistrations
{
    public void AddCqrsAssemblies(IServiceCollection services)
    {
        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(MyRegistrations).Assembly));
    }
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(ServiceRegistrationAnalyzer.UseAddMediatorExtensionMethodRule, startLine: 6, startColumn: 18, endLine: 6, endColumn: 28)
            .RunAsync();
    }
}