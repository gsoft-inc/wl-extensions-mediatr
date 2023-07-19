using MediatR;

namespace Workleap.Extensions.MediatR;

internal sealed class StreamRequestValidationBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        DataAnnotationsValidationHelper.Validate(request);
        return next();
    }
}