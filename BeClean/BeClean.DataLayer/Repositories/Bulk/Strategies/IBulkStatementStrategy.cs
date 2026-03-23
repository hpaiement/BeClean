using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    public interface IBulkStatementStrategy<TEntity, TDbContext>
        where TDbContext : DbContext
    {
        /// <summary>
        /// Return table name with schema name (if applicable)
        /// </summary>
        /// <returns></returns>
        string GetTableFullName();

        /// <summary>
        /// Returns a vendor-specific temp table name from a base name.
        /// For example, SQL Server requires a # prefix.
        /// </summary>
        /// <param name="baseName"></param>
        /// <returns></returns>
        string GetTempTableName(string baseName);

        /// <summary>
        /// Create a temp table for TEntity entity type. Can then be used for bulk operations (copy, merge, etc.)
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task CreateTempTableAsync(string tableName);

        /// <summary>
        /// Copy date from application to database server. Data will be copied to the tempTableName provided.
        /// </summary>
        /// <param name="tempTableName"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        Task BulkCopyToTempTableAsync(string tempTableName, IEnumerable<TEntity> items);

        /// <summary>
        /// Copy items to the db table with high performance. Ignore calculated fields like PK identity
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        Task BulkCopyToDbTableAsync(IEnumerable<TEntity> items);

        /// <summary>
        /// Upsert data from temporary table to the real table using compareProperties for match
        /// making
        /// </summary>
        /// <param name="tempTableName"></param>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        Task MergeTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null
        );

        /// <summary>
        /// Insert missing data from temporary table to the real table using compareProperties for match. No update is
        /// done on matching rows
        /// </summary>
        /// <param name="tempTableName"></param>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        Task MergeInsertTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties
        );
    }
}
