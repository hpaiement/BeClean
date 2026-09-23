using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using BeClean.DataLayer.Repositories.Bulk;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Repositories
{
    public class MappedItemRepository(
        ChinookContext dbContext,
        IChinookUnitOfWork unitOfWork
    ) : EFBulkRepository<MappedItem, ChinookContext>(dbContext, unitOfWork)
    {
        /// <summary>
        /// Custom bulk statement built on the <see cref="EFBulkRepository{TModel, TDbContext}.ExecuteThroughTempTableAsync"/>
        /// extension point: synchronizes items on Code, but only deletes missing rows of batches &gt;= <paramref name="fromBatch"/>
        /// </summary>
        public async Task SynchronizeFromBatchAsync(IEnumerable<MappedItem> items, int fromBatch)
        {
            await ExecuteThroughTempTableAsync(items, async tempTableName =>
            {
                var targetTable = BulkStrategy.GetTableFullName();
                var sourceTable = BulkStrategy.EncloseDbIdentifier(tempTableName);
                var onClause = BulkStrategy.GenerateMergeOnClause(i => i.Code);
                var batchColumn = BulkStrategy.EncloseDbIdentifier("import_batch");

                if (_dbContext.Database.IsNpgsql())
                {
                    // "WHEN NOT MATCHED BY SOURCE" is only supported since PostgreSQL 17
                    var deleteSql =
$@"DELETE FROM {targetTable} tgt
WHERE tgt.{batchColumn} >= {{0}} AND NOT EXISTS (
    SELECT 1 FROM {sourceTable} src
    WHERE {onClause}
);";
                    await _dbContext.Database.ExecuteSqlRawAsync(deleteSql, fromBatch);
                    await BulkStrategy.MergeTempTableAsync(tempTableName, i => i.Code);
                }
                else
                {
                    var mergeSql =
$@"MERGE INTO {targetTable} tgt
USING {sourceTable} src ON
{onClause}
{BulkStrategy.GenerateMergeWhenMatchedClause(i => i.Code)}
WHEN NOT MATCHED BY TARGET THEN
{BulkStrategy.GenerateMergeInsertStatement()}
WHEN NOT MATCHED BY SOURCE AND tgt.{batchColumn} >= {{0}} THEN
DELETE;";
                    await _dbContext.Database.ExecuteSqlRawAsync(mergeSql, fromBatch);
                }
            },
            nameof(SynchronizeFromBatchAsync));
        }
    }
}
