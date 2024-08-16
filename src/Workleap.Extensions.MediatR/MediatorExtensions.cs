using System.Diagnostics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace MediatR;

// These extension methods follows Microsoft guidelines in terms of asynchronous programming
// and also encourage the developer to provide the right cancellation token depending on the context.
// https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios#important-info-and-advice
[DebuggerStepThrough]
public static class MediatorExtensions
{
    public static Task<TResponse> SendAsync<TResponse>(this ISender sender, IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return sender.Send(request, cancellationToken);
    }

    public static Task SendAsync<TRequest>(this ISender sender, TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        return sender.Send(request, cancellationToken);
    }

    public static Task PublishAsync<TNotification>(this IPublisher publisher, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        return publisher.Publish(notification, cancellationToken);
    }
}