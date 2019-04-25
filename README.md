[projectUri]: https://github.com/sketch7/dotnet.multitenancy
[changeLog]: ./CHANGELOG.md

# Dotnet Multitenancy
[![CircleCI](https://circleci.com/gh/sketch7/dotnet.multitenancy.svg?style=shield)](https://circleci.com/gh/sketch7/dotnet.multitenancy)
<!-- TODO: [![NuGet version](https://badge.fury.io/nu/fluentlyhttpclient.svg)](https://badge.fury.io/nu/fluentlyhttpclient)  -->

Multitenancy library for .NET Standard

**Quick links**

[Change logs][changeLog] | [Project Repository][projectUri]

## Features
- Grace
- aspnet core support
- Microsoft Orleans support

## Installation
Available for [.NET Standard 2.0+](https://docs.microsoft.com/en-gb/dotnet/standard/net-standard)

### NuGet
```
PM> Install-Package FluentlyHttpClient
```

### csproj

```xml
<PackageReference Include="FluentlyHttpClient" Version="*" />
```

## Usage

### Configure
Add services via `.AddFluentlyHttpClient()`.

```cs
// using Startup.cs (can be elsewhere)
public void ConfigureServices(IServiceCollection services)
{
    services.AddFluentlyHttpClient();
}
```

Configure an Http client using the Http Factory (you need at least one).
```cs
// using Startup.cs (can be elsewhere)
public void Configure(IApplicationBuilder app, IFluentHttpClientFactory fluentHttpClientFactory)
{
  fluentHttpClientFactory.CreateBuilder(identifier: "platform") // keep a note of the identifier, its needed later
    .WithBaseUrl("http://sketch7.com") // required
    .WithHeader("user-agent", "slabs-testify")
    .WithTimeout(5)
    .UseMiddleware<LoggerHttpMiddleware>()
    .Register(); // register client builder to factory
}
```

### Basic usage

> todo

## Contributing

### Setup Machine for Development
Install/setup the following:

- NodeJS v8+
- Visual Studio Code or similar code editor

 ### Commands

```bash
# run tests
npm test

# bump version
npm version minor --no-git-tag # major | minor | patch | prerelease

# nuget pack (only)
npm run pack

# nuget publish dev (pack + publish + clean)
npm run publish:dev
```