#pragma warning disable SA1649: Having all the test requests and handlers in a single file is better for readability

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using MediatR;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed record SampleRequest([property: Required] string RequiredValue, bool IsSuccessful) : IRequest<string>;

internal sealed class SampleRequestHandler : IRequestHandler<SampleRequest, string>
{
    public Task<string> Handle(SampleRequest request, CancellationToken cancellationToken)
    {
        return request.IsSuccessful ? Task.FromResult($"Hello {request.RequiredValue}!") : throw new InvalidOperationException("Something wrong happened");
    }
}

internal sealed record SampleStreamRequest([property: Required] string RequiredValue, bool IsSuccessful) : IStreamRequest<string>;

internal sealed class SampleStreamQueryHandler : IStreamRequestHandler<SampleStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(SampleStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "Hello";

        if (!request.IsSuccessful)
        {
            throw new InvalidOperationException("Something wrong happened");
        }

        yield return request.RequiredValue + "!";
    }
}