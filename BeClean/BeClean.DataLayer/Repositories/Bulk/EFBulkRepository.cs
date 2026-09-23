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
        private IBulkStatementStrategy<TModel, TDbContext> BulkStrategy => _bulkStrategy ??= CreateBulkStrategy();

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
            Expression<Func<TModel, object>>? dontUpdateColumns = null,
            Expression<Func<TModel, bool>>? deleteScope = null)
        {
            await ExecuteThroughTempTableAsync(
                items,
                async tempTableName =>
                {
                    await DeleteRowsMissingFromTempTableAsync(tempTableName, compareProperties, deleteScope);
                    await BulkStrategy.MergeTempTableAsync(tempTableName, compareProperties, dontUpdateColumns);
                },
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
        /// In a transaction, copies <paramref name="items"/> to a new temp table, then runs <paramref name="operation"/>
        /// with the temp table name. The transaction is rolled back if the operation throws.
        /// </summary>
        /// <param name="items">Items copied to the temp table</param>
        /// <param name="operation">Statement(s) to run, receives the temp table name</param>
        /// <param name="operationName">Name used in the temp table name, for troubleshooting</param>
        /// <returns></returns>
        private async Task ExecuteThroughTempTableAsync(
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

        /// <summary>
        /// Deletes the table rows matching <paramref name="deleteScope"/> (all rows if null) that have no temp table
        /// row with the same <paramref name="compareProperties"/> values. Query filters are ignored so they do not
        /// silently narrow the delete.
        /// </summary>
        private async Task DeleteRowsMissingFromTempTableAsync(
            string tempTableName,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, bool>>? deleteScope)
        {
            var tempTableSql = "SELECT * FROM " + BulkStrategy.EncloseDbIdentifier(tempTableName);
            var tempTableRows = _dbSet.FromSqlRaw(tempTableSql).IgnoreQueryFilters();

            var rowsInScope = _dbSet.IgnoreQueryFilters();
            if (deleteScope != null)
                rowsInScope = rowsInScope.Where(deleteScope);

            await rowsInScope
                .Where(HasNoMatchIn(tempTableRows, compareProperties))
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Returns tgt => !rows.Any(src => tgt.Col1 == src.Col1 &amp;&amp; tgt.Col2 == src.Col2 ...) for compareProperties
        /// </summary>
        private static Expression<Func<TModel, bool>> HasNoMatchIn(
            IQueryable<TModel> rows,
            Expression<Func<TModel, object>> compareProperties)
        {
            var tgt = Expression.Parameter(typeof(TModel), "tgt");
            var src = Expression.Parameter(typeof(TModel), "src");

            var matchBody = PropertyExpression.GetProperties(compareProperties)
                .Select(p => ColumnsMatch(Expression.Property(tgt, p), Expression.Property(src, p)))
                .Aggregate(Expression.AndAlso);
            var match = Expression.Lambda<Func<TModel, bool>>(matchBody, src);

            var anyMatch = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Any),
                [typeof(TModel)],
                rows.Expression,
                Expression.Quote(match));

            return Expression.Lambda<Func<TModel, bool>>(Expression.Not(anyMatch), tgt);
        }

        /// <summary>
        /// tgt.Col == src.Col with the SQL semantics of the MERGE ON clause: a null value never matches, not even
        /// another null (EF translates == with C# semantics, null == null being true)
        /// </summary>
        private static Expression ColumnsMatch(MemberExpression tgtColumn, MemberExpression srcColumn)
        {
            var equal = Expression.Equal(tgtColumn, srcColumn);

            var isNullable = !tgtColumn.Type.IsValueType || Nullable.GetUnderlyingType(tgtColumn.Type) != null;
            if (!isNullable)
                return equal;

            return Expression.AndAlso(Expression.NotEqual(tgtColumn, Expression.Constant(null, tgtColumn.Type)), equal);
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
