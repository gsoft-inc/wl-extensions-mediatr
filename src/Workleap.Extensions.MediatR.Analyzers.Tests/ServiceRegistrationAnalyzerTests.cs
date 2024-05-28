namespace Workleap.Extensions.MediatR.Analyzers.Tests;

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
        services.{|#0:AddMediatR|}(x => x.RegisterServicesFromAssembly(typeof(MyRegistrations).Assembly));
    }
}";
        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(ServiceRegistrationAnalyzer.UseAddMediatorExtensionMethodRule, 0)
            .RunAsync();
    }
}