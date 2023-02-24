using Microsoft.CodeAnalysis;

namespace GSoft.Extensions.MediatR.Analyzers.Tests;

internal static class CompilationBuilderExtensions
{
    public static async Task ShouldCompileWithoutDiagnostics(this CompilationBuilder builder)
    {
        var diagnostics = await builder.Compile();
        Assert.Empty(diagnostics);
    }

    public static async Task ShouldCompileWithDiagnostic(this CompilationBuilder builder, DiagnosticDescriptor expectedDiagnostic)
    {
        var diagnostics = await builder.Compile();
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(expectedDiagnostic, diagnostic.Descriptor);
    }
}