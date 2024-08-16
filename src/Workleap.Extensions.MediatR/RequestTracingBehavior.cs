using System.Diagnostics;
using MediatR;

namespace Workleap.Extensions.MediatR;

internal sealed class RequestTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = TracingHelper.StartActivity();

        if (activity == null)
        {
            return await next().ConfigureAwait(false);
        }

        return await HandleWithTracing(request, next, activity).ConfigureAwait(false);
    }

    private static async Task<TResponse> HandleWithTracing(TRequest request, RequestHandlerDelegate<TResponse> next, Activity activity)
    {
        activity.DisplayName = request.GetType().Name;

        try
        {
            var result = await next().ConfigureAwait(false);
            TracingHelper.MarkAsSuccessful(activity);
            return result;
        }
        catch (Exception ex)
        {
            TracingHelper.MarkAsFailed(activity, ex);
            throw;
        }
    }
}