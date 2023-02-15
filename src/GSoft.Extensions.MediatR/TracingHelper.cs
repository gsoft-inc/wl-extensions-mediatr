using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GSoft.Extensions.MediatR;

internal static class TracingHelper
{
    // https://github.com/open-telemetry/opentelemetry-specification/blob/v1.18.0/specification/common/mapping-to-non-otlp.md#span-status
    internal const string StatusCodeTag = "otel.status_code";
    internal const string StatusDescriptionTag = "otel.status_description";

    // https://github.com/open-telemetry/opentelemetry-specification/blob/v1.18.0/specification/logs/semantic_conventions/exceptions.md
    internal const string ExceptionTypeTag = "exception.type";
    internal const string ExceptionMessageTag = "exception.message";
    internal const string ExceptionStackTraceTag = "exception.stacktrace";

    private const string ActivityName = "Mediator";

    private static readonly Assembly Assembly = typeof(TracingHelper).Assembly;
    private static readonly AssemblyName AssemblyName = Assembly.GetName();
    private static readonly string AssemblyVersion = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? AssemblyName.Version!.ToString();

    private static readonly ActivitySource ActivitySource = new ActivitySource(AssemblyName.Name!, AssemblyVersion);

    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument", Justification = "We want a specific activity name, not the caller method name")]
    public static Activity? StartActivity()
    {
        // Note that the activity kind is the default value: Internal
        return ActivitySource.StartActivity(ActivityName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkAsSuccessful(Activity activity)
    {
        activity.AddTag(StatusCodeTag, "OK");
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkAsFailed(Activity activity, Exception ex)
    {
        activity.AddTag(StatusCodeTag, "ERROR");
        activity.SetStatus(ActivityStatusCode.Error);

        if (activity.IsAllDataRequested)
        {
            activity.AddTag(StatusDescriptionTag, ex.Message);
            activity.AddTag(ExceptionTypeTag, ex.GetType().FullName);
            activity.AddTag(ExceptionMessageTag, ex.Message);
            activity.AddTag(ExceptionStackTraceTag, ex.StackTrace);
        }
    }
}