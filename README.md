# GSoft.Extensions.MediatR

[![nuget](https://img.shields.io/nuget/v/GSoft.Extensions.MediatR.svg?logo=nuget)](https://www.nuget.org/packages/GSoft.Extensions.MediatR/)
[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/gsoft-extensions-mediatr/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/gsoft-extensions-mediatr/actions/workflows/publish.yml)

MediatR extensions, behaviors and default configuration.


## Getting started

TODO documentation


## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.


## License

Copyright © 2023, GSoft Group Inc. This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
