# BeClean.DataLayer.PostgreSql

PostgreSQL bulk operations for `BeClean.DataLayer`'s `EFBulkRepository`: data is copied to a temp table with Npgsql binary import (`COPY ... FROM STDIN (FORMAT BINARY)`), then merged into the target table with a `MERGE` statement.

## Usage

Enable bulk operations when configuring the Npgsql provider:

```csharp
services.AddDbContext<MyContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseBeCleanBulk()));
```

## Requirements

- .NET 8+
- Entity Framework Core 8+
- PostgreSQL 15+ (`MERGE` support)
