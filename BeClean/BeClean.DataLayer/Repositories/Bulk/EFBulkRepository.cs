using BeClean.DataLayer.Repositories.Bulk.Strategies;
using BeClean.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace BeClean.DataLayer.Repositories.Bulk
{
    /// <summary>
    /// Bulk operations run raw SQL statements directly against the database and bypass the EF change tracker: entities
    /// already tracked by the db context are NOT refreshed and may be stale after a bulk operation. Use a new db context
    /// (or detach / reload the entities) to read the resulting data.
    /// </summary>
    public abstract class EFBulkRepository<TModel, TDbContext>(
        TDbContext dbContext,
        IUnitOfWork unitOfWork
    ) : EFRepository<TModel, TDbContext>(dbContext), IEFBulkRepository<TModel>
        where TModel : class
        where TDbContext : DbContext
    {
        protected readonly IUnitOfWork _unitOfWork = unitOfWork;
        private IBulkStatementStrategy<TModel, TDbContext>? _bulkStrategy;

        /// <summary>
        /// Database provider specific bulk strategy, resolved on first use so non bulk operations keep working on
        /// providers without bulk support (e.g. SQLite or InMemory in tests)
        /// </summary>
        /// <exception cref="InvalidOperationException">Bulk operations are not enabled on the db context options</exception>
        protected IBulkStatementStrategy<TModel, TDbContext> BulkStrategy => _bulkStrategy ??= CreateBulkStrategy();

        public async Task MergeInsertAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties
        )
        {
            await ExecuteThroughTempTableAsync(
                items,
                tempTableName => BulkStrategy.MergeInsertTempTableAsync(tempTableName, compareProperties),
                nameof(MergeInsertAsync));
        }

        public async Task MergeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
        {
            await ExecuteThroughTempTableAsync(
                items,
                tempTableName => BulkStrategy.MergeTempTableAsync(tempTableName, compareProperties, dontUpdateColumns),
                nameof(MergeAsync));
        }

        public async Task SynchronizeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
        {
            await ExecuteThroughTempTableAsync(
                items,
                tempTableName => BulkStrategy.SynchronizeTempTableAsync(tempTableName, compareProperties, dontUpdateColumns),
                nameof(SynchronizeAsync));
        }

        /// <summary>
        /// Generated keys (e.g. identity columns) are NOT written back to <paramref name="items"/>: read the inserted
        /// rows from the database if you need them.
        /// </summary>
        public async Task BulkInsertAsync(IEnumerable<TModel> items)
        {
            var bulkStrategy = BulkStrategy;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await bulkStrategy.BulkCopyToDbTableAsync(items);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task BulkUpdateAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
        {
            await ExecuteThroughTempTableAsync(
                items,
                tempTableName => BulkStrategy.MergeUpdateTempTableAsync(tempTableName, compareProperties, dontUpdateColumns),
                nameof(BulkUpdateAsync));
        }

        /// <summary>
        /// Extension point for custom bulk statements: in a transaction, copies <paramref name="items"/> to a new temp
        /// table, then runs <paramref name="operation"/> with the temp table name. Use <see cref="BulkStrategy"/> SQL
        /// helpers (GetTableFullName, GenerateMergeOnClause, ...) to build the statement. The transaction is rolled
        /// back if the operation throws.
        /// </summary>
        /// <param name="items">Items copied to the temp table</param>
        /// <param name="operation">Statement(s) to run, receives the temp table name</param>
        /// <param name="operationName">Name used in the temp table name, for troubleshooting</param>
        /// <returns></returns>
        protected async Task ExecuteThroughTempTableAsync(
            IEnumerable<TModel> items,
            Func<string, Task> operation,
            string operationName)
        {
            var bulkStrategy = BulkStrategy;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tempTableName = bulkStrategy.GetTempTableName($"{operationName}_{GetType().Name}_{Guid.NewGuid().ToString().Replace("-", "")}");
                await bulkStrategy.CreateTempTableAsync(tempTableName);
                await bulkStrategy.BulkCopyToTempTableAsync(tempTableName, items);
                await operation(tempTableName);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private IBulkStatementStrategy<TModel, TDbContext> CreateBulkStrategy()
        {
            var strategyFactory = _dbContext.GetInfrastructure().GetService<IBulkStatementStrategyFactory>()
                ?? throw new InvalidOperationException(
                    $"Bulk operations are not enabled for {typeof(TDbContext).Name} (database provider {_dbContext.Database.ProviderName}). " +
                    "Reference the BeClean.DataLayer.SqlServer or BeClean.DataLayer.PostgreSql package and call UseBeCleanBulk() " +
                    "when configuring the provider, e.g. options.UseSqlServer(connectionString, o => o.UseBeCleanBulk())");

            return strategyFactory.Create<TModel, TDbContext>(_dbContext);
        }
    }
}
