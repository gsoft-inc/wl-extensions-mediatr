using System.Collections.Immutable;
using GSoft.Extensions.MediatR.Analyzers.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GSoft.Extensions.MediatR.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamingConventionAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseCommandOrQuerySuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseCommandOrQuerySuffix,
        title: "Name should end with 'Command' or 'Query'",
        messageFormat: "Name should end with 'Command' or 'Query'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseCommandHandlerOrQueryHandlerSuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseCommandHandlerOrQueryHandlerSuffix,
        title: "Name should end with 'CommandHandler' or 'QueryHandler'",
        messageFormat: "Name should end with 'CommandHandler' or 'QueryHandler'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseStreamQuerySuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseStreamQuerySuffix,
        title: "Name should end with 'StreamQuery'",
        messageFormat: "Name should end with 'StreamQuery'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseStreamQueryHandlerSuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseStreamQueryHandlerSuffix,
        title: "Name should end with 'StreamQueryHandler'",
        messageFormat: "Name should end with 'StreamQueryHandler'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseNotificationOrEventSuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseNotificationOrEventSuffix,
        title: "Name should end with 'Notification' or 'Event'",
        messageFormat: "Name should end with 'Notification' or 'Event'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseNotificationHandlerOrEventHandlerSuffixRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseNotificationHandlerOrEventHandlerSuffix,
        title: "Name should end with 'NotificationHandler' or 'EventHandler'",
        messageFormat: "Name should end with 'NotificationHandler' or 'EventHandler'",
        category: RuleCategories.Naming,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseCommandOrQuerySuffixRule,
        UseCommandHandlerOrQueryHandlerSuffixRule,
        UseStreamQuerySuffixRule,
        UseStreamQueryHandlerSuffixRule,
        UseNotificationOrEventSuffixRule,
        UseNotificationHandlerOrEventHandlerSuffixRule);

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
            context.RegisterSymbolAction(analyzer.AnalyzeNamedType, SymbolKind.NamedType);
        }
    }

    private sealed class AnalyzerImplementation
    {
        private readonly HashSet<INamedTypeSymbol> _requestHandlerTypes;

        private readonly INamedTypeSymbol _baseRequestType;
        private readonly INamedTypeSymbol _notificationType;
        private readonly INamedTypeSymbol _notificationHandlerType;
        private readonly INamedTypeSymbol _streamRequestType;
        private readonly INamedTypeSymbol _streamRequestHandlerType;

        public AnalyzerImplementation(Compilation compilation)
        {
            this._requestHandlerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            this._baseRequestType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.BaseRequestInterface, KnownSymbolNames.MediatRContractsAssembly)!;
            this._notificationType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.NotificationInterface, KnownSymbolNames.MediatRContractsAssembly)!;
            this._notificationHandlerType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.NotificationHandlerInterface, KnownSymbolNames.MediatRAssembly)!;
            this._streamRequestType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.StreamRequestInterface, KnownSymbolNames.MediatRContractsAssembly)!;
            this._streamRequestHandlerType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.StreamRequestHandlerInterface, KnownSymbolNames.MediatRAssembly)!;

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT1) is { } requestHandlerTypeT1)
            {
                this._requestHandlerTypes.Add(requestHandlerTypeT1);
            }

            if (compilation.GetBestTypeByMetadataName(KnownSymbolNames.RequestHandlerInterfaceT2) is { } requestHandlerTypeT2)
            {
                this._requestHandlerTypes.Add(requestHandlerTypeT2);
            }
        }

        public bool IsValid =>
            this._baseRequestType != null &&
            this._notificationType != null &&
            this._notificationHandlerType != null &&
            this._streamRequestType != null &&
            this._streamRequestHandlerType != null &&
            this._requestHandlerTypes.Count == 2;

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } type)
            {
                this.AnalyzeBaseRequest(context, type);
                this.AnalyzeRequestHandler(context, type);

                this.AnalyzeStreamRequest(context, type);
                this.AnalyzeStreamRequestHandler(context, type);

                this.AnalyzeNotification(context, type);
                this.AnalyzeNotificationHandler(context, type);
            }
        }

        private void AnalyzeBaseRequest(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsBaseRequestInterface(type) && !NameEndsWithCommandOrQuery(type))
            {
                context.ReportDiagnostic(UseCommandOrQuerySuffixRule, type);
            }
        }

        private bool ImplementsBaseRequestInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this._baseRequestType));
        }

        private static bool NameEndsWithCommandOrQuery(ISymbol type)
        {
            return type.Name.EndsWith("Command", StringComparison.Ordinal) || type.Name.EndsWith("Query", StringComparison.Ordinal);
        }

        private void AnalyzeRequestHandler(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsRequestHandlerInterface(type) && !NameEndsWithCommandHandlerOrQueryHandler(type))
            {
                context.ReportDiagnostic(UseCommandHandlerOrQueryHandlerSuffixRule, type);
            }
        }

        private bool ImplementsRequestHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsRequestHandler);
        }

        private bool IsRequestHandler(INamedTypeSymbol type)
        {
            return type is { IsGenericType: true, Arity: 1 or 2 } && this._requestHandlerTypes.Contains(type.ConstructedFrom);
        }

        private static bool NameEndsWithCommandHandlerOrQueryHandler(ISymbol type)
        {
            return type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) || type.Name.EndsWith("QueryHandler", StringComparison.Ordinal);
        }

        private void AnalyzeStreamRequest(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsStreamRequestInterface(type) && !NameEndsWithStreamQuery(type))
            {
                context.ReportDiagnostic(UseStreamQuerySuffixRule, type);
            }
        }

        private bool ImplementsStreamRequestInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsStreamRequest);
        }

        private bool IsStreamRequest(INamedTypeSymbol type)
        {
            return type is { IsGenericType: true, Arity: 1 } && SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, this._streamRequestType);
        }

        private static bool NameEndsWithStreamQuery(ISymbol type)
        {
            return type.Name.EndsWith("StreamQuery", StringComparison.Ordinal);
        }

        private void AnalyzeStreamRequestHandler(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsStreamRequestHandlerInterface(type) && !NameEndsWithStreamQueryHandler(type))
            {
                context.ReportDiagnostic(UseStreamQueryHandlerSuffixRule, type);
            }
        }

        private bool ImplementsStreamRequestHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsStreamRequestHandler);
        }

        private bool IsStreamRequestHandler(INamedTypeSymbol type)
        {
            return type is { IsGenericType: true, Arity: 2 } && SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, this._streamRequestHandlerType);
        }

        private static bool NameEndsWithStreamQueryHandler(ISymbol type)
        {
            return type.Name.EndsWith("StreamQueryHandler", StringComparison.Ordinal);
        }

        private void AnalyzeNotification(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsNotificationInterface(type) && !NameEndsWithNotificationOrEvent(type))
            {
                context.ReportDiagnostic(UseNotificationOrEventSuffixRule, type);
            }
        }

        private bool ImplementsNotificationInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this._notificationType));
        }

        private static bool NameEndsWithNotificationOrEvent(ISymbol type)
        {
            return type.Name.EndsWith("Notification", StringComparison.Ordinal) || type.Name.EndsWith("Event", StringComparison.Ordinal);
        }

        private void AnalyzeNotificationHandler(SymbolAnalysisContext context, ITypeSymbol type)
        {
            if (this.ImplementsNotificationHandlerInterface(type) && !NameEndsWithNotificationHandlerOrEventHandler(type))
            {
                context.ReportDiagnostic(UseNotificationHandlerOrEventHandlerSuffixRule, type);
            }
        }

        private bool ImplementsNotificationHandlerInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(this.IsNotificationHandler);
        }

        private bool IsNotificationHandler(INamedTypeSymbol type)
        {
            return type is { IsGenericType: true, Arity: 1 } && SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, this._notificationHandlerType);
        }

        private static bool NameEndsWithNotificationHandlerOrEventHandler(ISymbol type)
        {
            return type.Name.EndsWith("NotificationHandler", StringComparison.Ordinal) || type.Name.EndsWith("EventHandler", StringComparison.Ordinal);
        }
    }
}