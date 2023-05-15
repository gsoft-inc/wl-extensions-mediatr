# GSoft.Extensions.MediatR

[![nuget](https://img.shields.io/nuget/v/GSoft.Extensions.MediatR.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.Extensions.MediatR/)
[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-extensions-mediatr/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-extensions-mediatr/actions/workflows/publish.yml)

This library ensures that [MediatR](https://github.com/jbogard/MediatR) is registered in the dependency injection container **as a singleton** and also adds several features:

* [Activity-based OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) instrumentation
* [High-performance logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) with `Debug` log level
* Data annotations support for request validation, similar to [ASP.NET Core model validation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
* [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview?tabs=net) instrumentation (in a [separate NuGet package](https://www.nuget.org/packages/GSoft.Extensions.MediatR.ApplicationInsights/))
* [CQRS](https://microservices.io/patterns/data/cqrs.html) conventions and MediatR best practices with Roslyn analyzers


## Getting started

Use the `AddMediator(params Assembly[] assemblies)` extension method on your dependency injection services (`IServiceCollection`) to automatically register all the MediatR request handlers from a given assembly.

```csharp
builder.Services.AddMediator(typeof(Program).Assembly /*, [more assemblies...] */);
```

If you use [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview?tabs=net) and want to instrument your handlers, you can install the dedicated [NuGet package](https://www.nuget.org/packages/GSoft.Extensions.MediatR.ApplicationInsights/):

```csharp
builder.Services.AddMediator(typeof(Program).Assembly).AddApplicationInsights();
```

There are multiple method overloads of `AddMediator`. For instance, you can override MediatR configuration using this overload that accepts a `Action<MediatRServiceConfiguration>`:

```csharp
builder.Services.AddMediator(
    cfg => cfg.NotificationPublisher = new TaskWhenAllPublisher(),
    typeof(Program).Assembly);
```


## Example

```csharp
// CQRS naming conventions are suggested by a Roslyn analyzer, but it can be disabled
public sealed record SayHelloCommand([property: Required] string To) : IRequest;

public sealed class SayHelloCommandHandler : IRequestHandler<SayHelloCommand>
{
    public Task Handle(SayHelloCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hello {command.To}!");
        return Task.CompletedTask;
    }
}

// [...] Retrieve an instance of IMediator or ISender
var mediator = serviceProvider.GetRequiredService<IMediator>();

// - We use the preferred Async-suffixed extension method to put emphasis on the asynchronous aspect of MediatR
// - A Roslyn analyzer suggests to specify a cancellation token, which is most of the time forgotten by developers
await mediator.SendAsync(new SayHelloCommand("world"), CancellationToken.None);

// This throws RequestValidationException because 'SayHelloCommand.To' is marked as required
await mediator.SendAsync(new SayHelloCommand(null!), CancellationToken.None);
```


## Included Roslyn analyzers

| Rule ID | Category | Severity | Description                                                  |
|---------|----------|----------|--------------------------------------------------------------|
| GMDTR01 | Naming   | Warning  | Name should end with 'Command' or 'Query'                    |
| GMDTR02 | Naming   | Warning  | Name should end with 'CommandHandler' or 'QueryHandler'      |
| GMDTR03 | Naming   | Warning  | Name should end with 'StreamQuery'                           |
| GMDTR04 | Naming   | Warning  | Name should end with 'StreamQueryHandler'                    |
| GMDTR05 | Naming   | Warning  | Name should end with 'Notification' or 'Event'               |
| GMDTR06 | Naming   | Warning  | Name should end with 'NotificationHandler' or 'EventHandler' |
| GMDTR07 | Design   | Warning  | Use generic method instead                                   |
| GMDTR08 | Design   | Warning  | Provide a cancellation token                                 |
| GMDTR09 | Design   | Warning  | Handlers should not call other handlers                      |
| GMDTR10 | Design   | Warning  | Handlers should not be public                                |
| GMDTR11 | Design   | Warning  | Use 'AddMediator' extension method instead of 'AddMediatR'   |
| GMDTR12 | Design   | Warning  | Use method ending with 'Async' instead                       |
| GMDTR13 | Naming   | Warning  | Name should end with 'Handler'                               |

In order to change the severity of one of these diagnostic rules, use a `.editorconfig` file, for instance:
```ini
[*.cs]
dotnet_diagnostic.GMDTR01.severity = none
```
To learn more about how to configure or suppress code analysis warnings, [read this documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings). 


## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.


## License

Copyright © 2023, GSoft Group Inc. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
