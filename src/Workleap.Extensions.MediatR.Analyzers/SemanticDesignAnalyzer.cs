﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Workleap.Extensions.MediatR.Analyzers.Internals;

namespace Workleap.Extensions.MediatR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SemanticDesignAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor HandlersShouldNotCallHandlerRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.RequestHandlersShouldNotCallHandler,
        title: "Request handlers should not call other handlers",
        messageFormat: "Request handlers should not call other handlers",
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
        private readonly ImmutableHashSet<INamedTypeSymbol> _notificationHandlerTypes;
        private readonly INamedTypeSymbol _genericRequestType;

        public AnalyzerImplementation(Compilation compilation)
        {
            var mediatorTypesWithSendOrSendAsyncMethodBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorClass, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.MediatorInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.SenderInterface, KnownSymbolNames.MediatRAssembly));
            mediatorTypesWithSendOrSendAsyncMethodBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.WorkleapMediatorExtensionsClass, KnownSymbolNames.WorkleapExtMediatRAssembly));
            this._mediatorTypesWithSendOrSendAsyncMethod = mediatorTypesWithSendOrSendAsyncMethodBuilder.ToImmutable();

            var requestHandlerTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            requestHandlerTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT1, KnownSymbolNames.MediatRAssembly));
            requestHandlerTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT2, KnownSymbolNames.MediatRAssembly));
            this._requestHandlerTypes = requestHandlerTypesBuilder.ToImmutable();

            var notificationHandlerTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            notificationHandlerTypesBuilder.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.NotificationHandlerInterface, KnownSymbolNames.MediatRAssembly));
            this._notificationHandlerTypes = notificationHandlerTypesBuilder.ToImmutable();

            this._genericRequestType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.GenericRequestInterface, KnownSymbolNames.MediatRContractsAssembly)!;
        }

        public bool IsValid => this._mediatorTypesWithSendOrSendAsyncMethod.Count == 4 && this._requestHandlerTypes.Count == 2 && this._notificationHandlerTypes.Count == 1 && this._genericRequestType != null;

        public void OnBlockStartAction(OperationBlockStartAnalysisContext context)
        {
            if (context.OwningSymbol is IMethodSymbol method && this.ImplementsHandlerInterface(method.ContainingType))
            {
                context.RegisterOperationAction(
                    operationContext => this.AnalyzeHandlerOperationInvocation(operationContext, method.ContainingType),
                    OperationKind.Invocation);
            }
        }

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } type)
            {
                if (this.ImplementsHandlerInterface(type) && type.DeclaredAccessibility == Accessibility.Public)
                {
                    context.ReportDiagnostic(HandlersShouldNotBePublicRule, type);
                }
            }
        }

        private bool ImplementsHandlerInterface(ITypeSymbol type)
        {
            return this.ImplementsRequestHandlerInterface(type) || this.ImplementsNotificationHandlerInterface(type);
        }

        private bool ImplementsRequestHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsRequestHandler);
        }

        private bool IsRequestHandler(INamedTypeSymbol symbol)
        {
            return symbol is { IsGenericType: true, Arity: 1 or 2 } &&
                   this._requestHandlerTypes.Contains(symbol.ConstructedFrom);
        }

        private bool ImplementsNotificationHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsNotificationHandler);
        }

        private bool IsNotificationHandler(INamedTypeSymbol symbol)
        {
            return symbol is { IsGenericType: true, Arity: 1 } &&
                   this._notificationHandlerTypes.Contains(symbol.ConstructedFrom);
        }

        private void AnalyzeHandlerOperationInvocation(OperationAnalysisContext context, ITypeSymbol containingType)
        {
            if (context.Operation is IInvocationOperation operation && this.IsMediatorSendMethodOrSendAsyncExtensionMethod(operation))
            {
                if (ContainingTypeNameEndsWithCommandHandler(context) && this.IsHandlingQueryArgument(operation))
                {
                    // This is fine, command handlers are allowed to consume query handlers
                    return;
                }

                if (this.ImplementsRequestHandlerInterface(containingType))
                {
                    context.ReportDiagnostic(HandlersShouldNotCallHandlerRule, operation);
                }
            }
        }

        private bool IsMediatorSendMethodOrSendAsyncExtensionMethod(IInvocationOperation operation)
        {
            // Detect Send(TRequest, CancellationToken) or SendAsync(ISender, TRequest, CancellationToken)
            return operation.Arguments.Length >= 2
                && this._mediatorTypesWithSendOrSendAsyncMethod.Contains(operation.TargetMethod.ContainingType)
                && MediatorSendAndSendAsyncMethodNames.Contains(operation.TargetMethod.Name);
        }

        private static bool ContainingTypeNameEndsWithCommandHandler(OperationAnalysisContext context)
        {
            return context.ContainingSymbol.ContainingType.Name.EndsWith("CommandHandler", StringComparison.Ordinal);
        }

        private bool IsHandlingQueryArgument(IInvocationOperation operation)
        {
            // When using built-in "Send" method, the first argument is the request
            // When using our "SendAsync" extension method, the first argument is a ISender and the second argument is the request
            var argument = operation.TargetMethod.Name == KnownSymbolNames.SendMethod ? operation.Arguments[0] : operation.Arguments[1];
            if (argument.Value.Type is not INamedTypeSymbol argumentType)
            {
                return false;
            }

            return this.IsGenericRequestInterface(argumentType) || this.ImplementsGenericRequestInterface(argumentType);
        }

        private bool ImplementsGenericRequestInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsGenericRequestInterface);
        }

        private bool IsGenericRequestInterface(INamedTypeSymbol type)
        {
            return type is { IsGenericType: true, Arity: 1 } && SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, this._genericRequestType);
        }
    }
}