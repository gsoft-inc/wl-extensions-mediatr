using MediatR;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GSoft.Extensions.MediatR.Analyzers.Tests;

public abstract class BaseAnalyzerTests<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected BaseAnalyzerTests()
    {
        const string globalUsings = @"
global using Microsoft.Extensions.DependencyInjection;
global using System.Runtime.CompilerServices;
global using MediatR;";

        this.Helper = new CompilationHelper()
            .WithAssemblyReference<IServiceCollection>() // Microsoft.Extensions.DependencyInjection
            .WithAssemblyReference<IMediator>() // MediatR and MediatR.Contracts
            .WithAssemblyReference(typeof(MediatorExtensions).Assembly) // GSoft.Extensions.MediatR (our assembly)
            .WithSourceFile("MediatRGlobalUsings.cs", globalUsings)
            .WithAnalyzer<TAnalyzer>();
    }

    internal CompilationHelper Helper { get; }
}