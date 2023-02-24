namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public sealed class ServiceRegistrationAnalyzerTests : BaseAnalyzerTests<ServiceRegistrationAnalyzer>
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
        await this.Builder.WithSourceFile(source).ShouldCompileWithDiagnostic(ServiceRegistrationAnalyzer.UseAddMediatorExtensionMethodRule);
    }
}