# BeClean.Localization

Localization support for user-facing messages and exceptions in .NET applications.

## Features

- **`LocaleStringService`** — thin wrapper around `IStringLocalizer` for retrieving localized strings by key
- **`LocalizedUserException`** — user-facing exception that carries a localization key and optional format arguments
- **`LocalizedUnauthorizedException`** — extends `UnauthorizedAccessException` with the same localization support, for permission and authentication errors

## Requirements

- .NET 10+
- `Microsoft.Extensions.Localization`
