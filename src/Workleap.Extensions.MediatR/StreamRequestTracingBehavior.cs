using System.Diagnostics;
using System.Runtime.CompilerServices;
using MediatR;

namespace Workleap.Extensions.MediatR;

internal sealed class StreamRequestTracingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var activity = TracingHelper.StartActivity();
        return activity == null ? next() : HandleWithTracing(request, next, activity, cancellationToken);
    }

    private static async IAsyncEnumerable<TResponse> HandleWithTracing(TRequest request, StreamHandlerDelegate<TResponse> next, Activity activity, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            activity.DisplayName = request.GetType().Name;

            IAsyncEnumerator<TResponse> resultsEnumerator;

            try
            {
                resultsEnumerator = next().GetAsyncEnumerator(cancellationToken);
            }
            catch (Exception ex)
            {
                TracingHelper.MarkAsFailed(activity, ex);
                throw;
            }

            await using (resultsEnumerator.ConfigureAwait(false))
            {
                var hasNext = true;

                while (hasNext)
                {
                    try
                    {
                        hasNext = await resultsEnumerator.MoveNextAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        TracingHelper.MarkAsFailed(activity, ex);
                        throw;
                    }

                    if (hasNext)
                    {
                        yield return resultsEnumerator.Current;
                    }
                }
            }

            TracingHelper.MarkAsSuccessful(activity);
        }
        finally
        {
            activity.ExecuteAsCurrentActivity(activity, static x => x.Dispose());
        }
    }
}