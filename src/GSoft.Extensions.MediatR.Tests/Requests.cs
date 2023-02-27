#pragma warning disable SA1649: Having all the test requests and handlers in a single file is better for readability

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using MediatR;

namespace GSoft.Extensions.MediatR.Tests;

internal sealed record SampleQuery([property: Required] string RequiredValue, bool IsSuccessful) : IRequest<string>;

internal sealed class SampleQueryHandler : IRequestHandler<SampleQuery, string>
{
    public Task<string> Handle(SampleQuery query, CancellationToken cancellationToken)
    {
        return query.IsSuccessful ? Task.FromResult($"Hello {query.RequiredValue}!") : throw new InvalidOperationException("Something wrong happened");
    }
}

internal sealed record SampleCommand([property: Required] string RequiredValue, bool IsSuccessful) : IRequest;

internal sealed class SampleCommandHandler : IRequestHandler<SampleCommand>
{
    public Task Handle(SampleCommand command, CancellationToken cancellationToken)
    {
        return command.IsSuccessful ? Task.CompletedTask : throw new InvalidOperationException("Something wrong happened");
    }
}

internal sealed record SampleStreamQuery([property: Required] string RequiredValue, bool IsSuccessful) : IStreamRequest<string>;

internal sealed class SampleStreamQueryHandler : IStreamRequestHandler<SampleStreamQuery, string>
{
    public async IAsyncEnumerable<string> Handle(SampleStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "Hello";

        if (!query.IsSuccessful)
        {
            throw new InvalidOperationException("Something wrong happened");
        }

        yield return query.RequiredValue + "!";
    }
}