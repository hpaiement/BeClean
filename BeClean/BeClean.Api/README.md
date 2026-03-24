# BeClean.Api

ASP.NET Core building blocks for clean architecture backend applications. Provides exception handling middleware, custom JSON converters, Swagger extensions, and a version endpoint.

## Features

- **Exception middleware** — `ExceptionMiddleware` catches unhandled exceptions and delegates them to `IExceptionHandler` implementations. Includes a built-in handler for EF Core exceptions, `LocalizedUserException`, and `LocalizedUnauthorizedException` (with automatic transaction rollback via `IUnitOfWorkRegistry`)
- **JSON converters** — `DateTimeConverter` and `DateTimeOffsetConverter` with configurable format patterns
- **Swagger** — `LanguageHeaderFilter` adds an `Accept-Language` header parameter to all Swagger operations
- **Version endpoint** — `WebApplicationExtensions.MapVersionEndpoint` exposes a `/version` route that reads build metadata from a `version.json` file

## Requirements

- .NET 10+
- ASP.NET Core 10+
