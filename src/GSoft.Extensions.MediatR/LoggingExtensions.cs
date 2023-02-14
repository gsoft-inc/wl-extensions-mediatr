using Microsoft.Extensions.Logging;

namespace GSoft.Extensions.MediatR;

// High-performance logging to prevent too many allocations
// https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
internal static partial class LoggingExtensions
{
    // RequestLoggingBehavior
    [LoggerMessage(1, LogLevel.Debug, "Request {RequestName} started")]
    public static partial void RequestStarted(this ILogger logger, string requestName);

    [LoggerMessage(2, LogLevel.Debug, "Request {RequestName} ended successfully after {Duration} seconds")]
    public static partial void RequestSucceeded(this ILogger logger, string requestName, double duration);

    [LoggerMessage(3, LogLevel.Debug, "Request {RequestName} failed after {Duration} seconds")]
    public static partial void RequestFailed(this ILogger logger, Exception ex, string requestName, double duration);

    // StreamRequestLoggingBehavior
    [LoggerMessage(4, LogLevel.Debug, "Stream request {RequestName} started")]
    public static partial void StreamRequestStarted(this ILogger logger, string requestName);

    [LoggerMessage(5, LogLevel.Debug, "Stream request {RequestName} ended successfully after {Duration} seconds")]
    public static partial void StreamRequestSucceeded(this ILogger logger, string requestName, double duration);

    [LoggerMessage(6, LogLevel.Debug, "Stream request {RequestName} failed after {Duration} seconds")]
    public static partial void StreamRequestFailed(this ILogger logger, Exception ex, string requestName, double duration);
}