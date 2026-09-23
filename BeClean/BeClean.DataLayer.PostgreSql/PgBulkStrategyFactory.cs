using BeClean.DataLayer.Repositories.Bulk.Strategies;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.PostgreSql
{
    /// <summary>
    /// Creates PostgreSQL bulk strategies. Registered with <see cref="NpgsqlDbContextOptionsBuilderExtensions.UseBeCleanBulk"/>
    /// </summary>
    public class PgBulkStrategyFactory : IBulkStatementStrategyFactory
    {
        public IBulkStatementStrategy<TEntity, TDbContext> Create<TEntity, TDbContext>(TDbContext dbContext)
            where TDbContext : DbContext
            => new PgBulkStrategy<TEntity, TDbContext>(dbContext);
    }
}
