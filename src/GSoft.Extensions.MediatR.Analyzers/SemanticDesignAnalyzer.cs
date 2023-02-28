using System.Collections.Immutable;
using GSoft.Extensions.MediatR.Analyzers.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GSoft.Extensions.MediatR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SemanticDesignAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor HandlersShouldNotCallHandlerRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.HandlersShouldNotCallHandler,
        title: "Handlers should not call other handlers",
        messageFormat: "Handlers should not call other handlers",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor HandlersShouldNotBePublicRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.HandlersShouldNotBePublic,
        title: "Handlers should not be public",
        messageFormat: "Handlers should not be public",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        HandlersShouldNotCallHandlerRule,
        HandlersShouldNotBePublicRule);

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
            context.RegisterOperationBlockStartAction(analyzer.OnBlockStartAction);
            context.RegisterSymbolAction(analyzer.AnalyzeNamedType, SymbolKind.NamedType);
        }
    }

    private sealed class AnalyzerImplementation
    {
        private static readonly HashSet<string> MediatorSendAndSendAsyncMethodNames = new HashSet<string>(StringComparer.Ordinal)
        {
            KnownSymbolNames.SendMethod,
            KnownSymbolNames.SendAsyncMethod,
        };

        private readonly ImmutableHashSet<INamedTypeSymbol> _mediatorTypesWithSendOrSendAsyncMethod;
        private readonly ImmutableHashSet<INamedTypeSymbol> _requestHandlerTypes;

        public AnalyzerImplementation(Compilation compilation)
        {
            var mediatorTypesWithSendOrSendAsyncMethodBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorClass, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.SenderInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.GSoftMediatorExtensionsClass, KnownSymbolNames.GSoftExtMediatRAssembly));
            this._mediatorTypesWithSendOrSendAsyncMethod = mediatorTypesWithSendOrSendAsyncMethodBuilder.ToImmutable();

            var requestHandlerTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            requestHandlerTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT1, KnownSymbolNames.MediatRAssembly));
            requestHandlerTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT2, KnownSymbolNames.MediatRAssembly));
            this._requestHandlerTypes = requestHandlerTypesBuilder.ToImmutable();
        }

        public bool IsValid => this._mediatorTypesWithSendOrSendAsyncMethod.Count == 4 && this._requestHandlerTypes.Count == 2;

        public void OnBlockStartAction(OperationBlockStartAnalysisContext context)
        {
            if (context.OwningSymbol is IMethodSymbol method && this.ImplementsRequestHandlerInterface(method.ContainingType))
            {
                context.RegisterOperationAction(this.AnalyzeOperationInvocation, OperationKind.Invocation);
            }
        }

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } type)
            {
                if (this.ImplementsRequestHandlerInterface(type) && type.DeclaredAccessibility == Accessibility.Public)
                {
                    context.ReportDiagnostic(HandlersShouldNotBePublicRule, type);
                }
            }
        }

        private bool ImplementsRequestHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsRequestHandler);
        }

        private bool IsRequestHandler(INamedTypeSymbol symbol)
        {
            return symbol is { IsGenericType: true, Arity: 1 or 2 } && this._requestHandlerTypes.Contains(symbol.ConstructedFrom);
        }

        private void AnalyzeOperationInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is IInvocationOperation operation && this.IsMediatorSendMethodOrSendAsyncExtensionMethod(operation))
            {
                context.ReportDiagnostic(HandlersShouldNotCallHandlerRule, operation);
            }
        }

        private bool IsMediatorSendMethodOrSendAsyncExtensionMethod(IInvocationOperation operation)
        {
            return this._mediatorTypesWithSendOrSendAsyncMethod.Contains(operation.TargetMethod.ContainingType) && MediatorSendAndSendAsyncMethodNames.Contains(operation.TargetMethod.Name);
        }
    }
}