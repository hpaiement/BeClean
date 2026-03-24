# BeClean.Repository

Generic repository and unit-of-work contracts for clean architecture .NET applications. This package contains only interfaces — it has no dependencies on any ORM or database technology, making it suitable as the innermost layer of a clean architecture.

## Interfaces

- **`IReadRepository<TModel>`** — query operations: `GetAsync`, `GetAllAsync`, `GetManyAsync`, `GetTotalRowCountAsync`
- **`IWriteRepository<TModel>`** — write operations: `InsertAsync`, `InsertManyAsync`, `UpdateAsync`, `UpdatePartialAsync`, `DeleteAsync`, `DeleteManyAsync`
- **`IRepository<TModel>`** — combines read and write
- **`IEFBulkRepository<TModel>`** — extends `IRepository` with bulk operations: `MergeInsertAsync`, `MergeAsync`, `BulkInsertAsync`
- **`IUnitOfWork`** — transaction management: `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`
- **`IUnitOfWorkRegistry`** — coordinated rollback across multiple DbContexts

## Requirements

- .NET 10+
