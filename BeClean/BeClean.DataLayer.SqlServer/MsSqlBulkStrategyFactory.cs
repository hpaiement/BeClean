using BeClean.DataLayer.Repositories.Bulk.Strategies;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.SqlServer
{
    /// <summary>
    /// Creates SQL Server bulk strategies. Registered with <see cref="SqlServerDbContextOptionsBuilderExtensions.UseBeCleanBulk"/>
    /// </summary>
    public class MsSqlBulkStrategyFactory : IBulkStatementStrategyFactory
    {
        public IBulkStatementStrategy<TEntity, TDbContext> Create<TEntity, TDbContext>(TDbContext dbContext)
            where TDbContext : DbContext
            => new MsSqlBulkStrategy<TEntity, TDbContext>(dbContext);
    }
}
