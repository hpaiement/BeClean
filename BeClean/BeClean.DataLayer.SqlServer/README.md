# BeClean.DataLayer.SqlServer

SQL Server bulk operations for `BeClean.DataLayer`'s `EFBulkRepository`: data is copied to a temp table with `SqlBulkCopy`, then merged into the target table with a `MERGE` statement.

## Usage

Enable bulk operations when configuring the SQL Server provider:

```csharp
services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(connectionString, o => o.UseBeCleanBulk()));
```

## Requirements

- .NET 8+
- Entity Framework Core 8+
- SQL Server 2008+
