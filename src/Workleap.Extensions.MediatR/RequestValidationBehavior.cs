using MediatR;

namespace Workleap.Extensions.MediatR;

internal sealed class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        DataAnnotationsValidationHelper.Validate(request);
        return next();
    }
}