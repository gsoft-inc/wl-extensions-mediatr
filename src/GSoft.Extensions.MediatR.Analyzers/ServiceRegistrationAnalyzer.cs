using System.Collections.Immutable;
using GSoft.Extensions.MediatR.Analyzers.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GSoft.Extensions.MediatR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceRegistrationAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseAddMediatorExtensionMethodRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseAddMediatorExtensionMethod,
        title: "Use 'AddMediator' extension method instead of 'AddMediatR'",
        messageFormat: "Use 'AddMediator' extension method instead of 'AddMediatR'",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UseAddMediatorExtensionMethodRule);

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
        private readonly INamedTypeSymbol _serviceCollectionType;
        private readonly INamedTypeSymbol _serviceCollectionExtensionsType;

        public AnalyzerImplementation(Compilation compilation)
        {
            this._serviceCollectionType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.ServiceCollectionInterface, KnownSymbolNames.MsExtDIAbstractionsAssembly)!;
            this._serviceCollectionExtensionsType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.ServiceCollectionExtensionsClass, KnownSymbolNames.MediatRAssembly)!;
        }

        public bool IsValid => this._serviceCollectionExtensionsType != null;

        public void AnalyzeOperationInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is IInvocationOperation operation && this.IsAddMediatRServiceCollectionExtensionMethod(operation))
            {
                context.ReportDiagnostic(UseAddMediatorExtensionMethodRule, operation);
            }
        }

        private bool IsAddMediatRServiceCollectionExtensionMethod(IInvocationOperation operation)
        {
            return operation.TargetMethod.ContainingType.IsStatic
                && operation.TargetMethod.Parameters.Length >= 1
                && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType, this._serviceCollectionExtensionsType)
                && operation.TargetMethod.Name == "AddMediatR"
                && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.Parameters[0].Type, this._serviceCollectionType);
        }
    }
}