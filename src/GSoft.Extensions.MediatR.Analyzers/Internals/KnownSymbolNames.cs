namespace GSoft.Extensions.MediatR.Analyzers.Internals;

internal static class KnownSymbolNames
{
    public const string MsExtDIAbstractionsAssembly = "Microsoft.Extensions.DependencyInjection.Abstractions";
    public const string MediatRAssembly = "MediatR";
    public const string MediatRContractsAssembly = "MediatR.Contracts";
    public const string GSoftExtMediatRAssembly = "GSoft.Extensions.MediatR";

    public const string ServiceCollectionInterface = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string ServiceCollectionExtensionsClass = "Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions";
    public const string GSoftMediatorExtensionsClass = "MediatR.MediatorExtensions";

    public const string BaseRequestInterface = "MediatR.IBaseRequest";
    public const string GenericRequestInterface = "MediatR.IRequest`1";
    public const string RequestHandlerInterfaceT1 = "MediatR.IRequestHandler`1";
    public const string RequestHandlerInterfaceT2 = "MediatR.IRequestHandler`2";

    public const string NotificationInterface = "MediatR.INotification";
    public const string NotificationHandlerInterface = "MediatR.INotificationHandler`1";

    public const string StreamRequestInterface = "MediatR.IStreamRequest`1";
    public const string StreamRequestHandlerInterface = "MediatR.IStreamRequestHandler`2";

    public const string MediatorClass = "MediatR.Mediator";
    public const string MediatorInterface = "MediatR.IMediator";
    public const string SenderInterface = "MediatR.ISender";
    public const string PublisherInterface = "MediatR.IPublisher";

    public const string SendMethod = "Send";
    public const string PublishMethod = "Publish";
    public const string CreateStreamMethod = "CreateStream";
    public const string SendAsyncMethod = "SendAsync";
}