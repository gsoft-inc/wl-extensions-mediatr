using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Workleap.Extensions.MediatR.Analyzers.Internals;

namespace Workleap.Extensions.MediatR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterUsageAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseGenericParameterRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseGenericParameter,
        title: "Use generic method instead",
        messageFormat: "Use generic method instead",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor ProvideCancellationTokenRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.ProvideCancellationToken,
        title: "Provide a cancellation token",
        messageFormat: "Provide a cancellation token",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseMethodEndingWithAsyncRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseMethodEndingWithAsync,
        title: "Use method ending with 'Async' instead",
        messageFormat: "Use method ending with 'Async' instead",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseGenericParameterRule,
        ProvideCancellationTokenRule,
        UseMethodEndingWithAsyncRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStarted);
    }

    private static void OnCompilationStarted(CompilationStartAnalysisContext context)
    {
        var analyzer = new AnalyzerImplementation(context.Compilation);
        if (analyzer.IsValid)
        {
            context.RegisterOperationAction(analyzer.AnalyzeOperationInvocation, OperationKind.Invocation);
        }
    }

    private sealed class AnalyzerImplementation
    {
        private static readonly HashSet<string> MediatorMethodNames = new HashSet<string>(StringComparer.Ordinal)
        {
            KnownSymbolNames.SendMethod,
            KnownSymbolNames.PublishMethod,
            KnownSymbolNames.CreateStreamMethod,
        };

        private static readonly HashSet<string> MediatorMethodNamesSupportingAsyncSuffix = new HashSet<string>(StringComparer.Ordinal)
        {
            KnownSymbolNames.SendMethod,
            KnownSymbolNames.PublishMethod,
        };

        private readonly ImmutableHashSet<INamedTypeSymbol> _mediatorTypes;

        public AnalyzerImplementation(Compilation compilation)
        {
            var mediatorTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            mediatorTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorClass, KnownSymbolNames.MediatRAssembly));
            mediatorTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.SenderInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.PublisherInterface, KnownSymbolNames.MediatRAssembly));
            this._mediatorTypes = mediatorTypesBuilder.ToImmutable();
        }

        public bool IsValid => this._mediatorTypes.Count == 4;

        public void AnalyzeOperationInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation operation)
            {
                return;
            }

            if (!this.IsMediatorMethod(operation))
            {
                return;
            }

            if (MediatorMethodNamesSupportingAsyncSuffix.Contains(operation.TargetMethod.Name))
            {
                context.ReportDiagnostic(UseMethodEndingWithAsyncRule, operation);
            }

            if (!operation.TargetMethod.IsGenericMethod)
            {
                context.ReportDiagnostic(UseGenericParameterRule, operation);
            }

            if (IsDefaultCancellationTokenParameter(operation))
            {
                context.ReportDiagnostic(ProvideCancellationTokenRule, operation);
            }
        }

        private bool IsMediatorMethod(IInvocationOperation operation)
        {
            return operation.TargetMethod.Parameters.Length == 2
                && this._mediatorTypes.Contains(operation.TargetMethod.ContainingType)
                && MediatorMethodNames.Contains(operation.TargetMethod.Name);
        }

        private static bool IsDefaultCancellationTokenParameter(IInvocationOperation operation)
        {
            return operation.Arguments.Length == 2 && operation.Arguments[1].ArgumentKind == ArgumentKind.DefaultValue;
        }
    }
}