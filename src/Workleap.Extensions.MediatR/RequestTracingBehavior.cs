using System.Diagnostics;
using MediatR;

namespace Workleap.Extensions.MediatR;

internal sealed class RequestTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var activity = TracingHelper.StartActivity();
        return activity == null ? next() : HandleWithTracing(request, next, activity);
    }

    private static async Task<TResponse> HandleWithTracing(TRequest request, RequestHandlerDelegate<TResponse> next, Activity activity)
    {
        using (activity)
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
}