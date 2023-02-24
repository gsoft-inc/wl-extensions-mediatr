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
        private readonly HashSet<INamedTypeSymbol> _mediatorTypesWithSendMethod;
        private readonly HashSet<INamedTypeSymbol> _requestHandlerTypes;

        public AnalyzerImplementation(Compilation compilation)
        {
            this._mediatorTypesWithSendMethod = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            this._requestHandlerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorClass, KnownSymbolNames.MediatRAssembly) is { } mediatorClassSymbol)
            {
                this._mediatorTypesWithSendMethod.Add(mediatorClassSymbol);
            }

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorInterface, KnownSymbolNames.MediatRAssembly) is { } mediatorInterfaceSymbol)
            {
                this._mediatorTypesWithSendMethod.Add(mediatorInterfaceSymbol);
            }

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.SenderInterface, KnownSymbolNames.MediatRAssembly) is { } senderInterfaceSymbol)
            {
                this._mediatorTypesWithSendMethod.Add(senderInterfaceSymbol);
            }

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT1, KnownSymbolNames.MediatRAssembly) is { } requestHandlerTypeT1)
            {
                this._requestHandlerTypes.Add(requestHandlerTypeT1);
            }

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT2, KnownSymbolNames.MediatRAssembly) is { } requestHandlerTypeT2)
            {
                this._requestHandlerTypes.Add(requestHandlerTypeT2);
            }
        }

        public bool IsValid => this._mediatorTypesWithSendMethod.Count == 3 && this._requestHandlerTypes.Count == 2;

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
            if (context.Operation is IInvocationOperation operation && this.IsMediatorSendMethod(operation))
            {
                context.ReportDiagnostic(HandlersShouldNotCallHandlerRule, operation);
            }
        }

        private bool IsMediatorSendMethod(IInvocationOperation operation)
        {
            return this._mediatorTypesWithSendMethod.Contains(operation.TargetMethod.ContainingType) && operation.TargetMethod is { Name: KnownSymbolNames.SendMethod };
        }
    }
}