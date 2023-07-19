using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Workleap.Extensions.MediatR.Analyzers.Internals;

internal static class RoslynExtensions
{
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string typeMetadataName, string? assemblyName = null)
    {
        var assemblies = GetAllAssemblies(compilation);

        if (assemblyName != null)
        {
            assemblies = assemblies.Where(x => x.Name == assemblyName);
        }

        foreach (var assembly in assemblies)
        {
            if (assembly.GetTypeByMetadataName(typeMetadataName) is { } symbol)
            {
                return symbol;
            }
        }

        return null;
    }

    private static IEnumerable<IAssemblySymbol> GetAllAssemblies(Compilation compilation)
    {
        yield return compilation.Assembly;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
            {
                yield return assembly;
            }
        }
    }

    public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor, ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location));
        }
    }

    public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor, IInvocationOperation operation)
    {
        var location = operation.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax memberAccessExpression
            ? memberAccessExpression.Name.GetLocation()
            : Location.None;

        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location));
    }
}