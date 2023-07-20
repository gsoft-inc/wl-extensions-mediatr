namespace Workleap.Extensions.MediatR;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string requestName, Type requestType, IEnumerable<string> failureMessages)
    {
        this.RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        this.RequestName = requestName ?? throw new ArgumentNullException(nameof(requestName));
        this.Failures = failureMessages ?? throw new ArgumentNullException(nameof(failureMessages));
    }

    public string RequestName { get; }

    public Type RequestType { get; }

    public IEnumerable<string> Failures { get; }

    public override string Message => string.Join("; ", this.Failures);
}