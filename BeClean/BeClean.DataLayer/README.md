# BeClean.DataLayer

Entity Framework Core implementation of the `BeClean.Repository` contracts. Provides generic repositories, a unit of work with savepoint support, and high-performance bulk operations for both SQL Server and PostgreSQL.

## Features

- **`EFReadRepository<TModel, TDbContext>`** — read-only EF Core repository with dynamic filtering via `GetManyAsync`
- **`EFRepository<TModel, TDbContext>`** — full CRUD repository
- **`EFBulkRepository<TModel, TDbContext>`** — bulk operations backed by database-specific strategies:
  - SQL Server: `SqlBulkCopy`-based merge/upsert
  - PostgreSQL: Npgsql binary import
- **`UnitOfWork`** — wraps EF Core transactions with savepoint support
- **`UnitOfWorkRegistry`** — coordinates rollback across multiple DbContexts
- **`MemoryRepository<TModel>`** — in-memory implementation for unit testing

## Requirements

- .NET 10+
- Entity Framework Core 10+
- SQL Server or PostgreSQL
