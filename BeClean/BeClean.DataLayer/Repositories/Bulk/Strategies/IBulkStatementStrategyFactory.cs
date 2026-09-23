using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    /// <summary>
    /// Creates the database provider specific bulk strategy. Registered in the EF internal service provider by the
    /// provider packages "UseBeCleanBulk()" options extension (BeClean.DataLayer.SqlServer, BeClean.DataLayer.PostgreSql)
    /// </summary>
    public interface IBulkStatementStrategyFactory
    {
        IBulkStatementStrategy<TEntity, TDbContext> Create<TEntity, TDbContext>(TDbContext dbContext)
            where TDbContext : DbContext;
    }
}
