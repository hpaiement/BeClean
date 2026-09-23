# BeClean.DataLayer

Entity Framework Core implementation of the `BeClean.Repository` contracts. Provides generic repositories, a unit of work with savepoint support, and high-performance bulk operations for both SQL Server and PostgreSQL (with the `BeClean.DataLayer.SqlServer` or `BeClean.DataLayer.PostgreSql` package).

## Features

- **`EFReadRepository<TModel, TDbContext>`** — read-only EF Core repository with dynamic filtering via `GetManyAsync`
- **`EFRepository<TModel, TDbContext>`** — full CRUD repository
- **`EFBulkRepository<TModel, TDbContext>`** — bulk operations backed by database-specific strategies:
  - SQL Server (`BeClean.DataLayer.SqlServer`): `SqlBulkCopy`-based merge/upsert
  - PostgreSQL (`BeClean.DataLayer.PostgreSql`): Npgsql binary import
- **`UnitOfWork`** — wraps EF Core transactions with savepoint support
- **`UnitOfWorkRegistry`** — coordinates rollback across multiple DbContexts
- **`MemoryRepository<TModel>`** — in-memory implementation for unit testing

## Bulk operations

Reference the provider package and enable bulk operations on the db context options:

```csharp
options.UseSqlServer(connectionString, o => o.UseBeCleanBulk());
// or
options.UseNpgsql(connectionString, o => o.UseBeCleanBulk());
```

Without `UseBeCleanBulk()`, bulk methods throw an `InvalidOperationException` while the other repository methods keep working (e.g. on SQLite or InMemory in tests).

Bulk operations bypass the EF change tracker: tracked entities are not refreshed, and `BulkInsertAsync` does not write generated keys (identity columns) back to the inserted items.

## Requirements

- .NET 10+
- Entity Framework Core 10+
- SQL Server or PostgreSQL
