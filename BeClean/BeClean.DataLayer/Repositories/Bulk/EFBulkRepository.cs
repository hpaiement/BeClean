using BeClean.DataLayer.Repositories.Bulk.Strategies;
using BeClean.Repository;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace BeClean.DataLayer.Repositories.Bulk
{
    public abstract class EFBulkRepository<TModel, TDbContext>(
        TDbContext dbContext,
        IUnitOfWork unitOfWork
    ) : EFRepository<TModel, TDbContext>(dbContext), IEFBulkRepository<TModel>
        where TModel : class
        where TDbContext : DbContext
    {
        protected readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IBulkStatementStrategy<TModel, TDbContext> _bulkStrategy = dbContext.Database.ProviderName switch
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL" => new PgBulkStrategy<TModel, TDbContext>(dbContext),
            "Microsoft.EntityFrameworkCore.SqlServer" => new MsSqlBulkStrategy<TModel, TDbContext>(dbContext),
            _ => throw new NotSupportedException($"Database provider {dbContext.Database.ProviderName} is not supported for bulk operations")
        };

        public async Task MergeInsertAsync(
            IEnumerable<TModel> items, 
            Expression<Func<TModel, object>> compareProperties, 
            bool ignoreTracker = true
        )
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tempTableName = _bulkStrategy.GetTempTableName($"{nameof(MergeInsertAsync)}_{GetType().Name}_{Guid.NewGuid().ToString().Replace("-", "")}");
                await _bulkStrategy.CreateTempTableAsync(tempTableName);
                await _bulkStrategy.BulkCopyToTempTableAsync(tempTableName, items);
                await _bulkStrategy.MergeInsertTempTableAsync(tempTableName, compareProperties);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (!ignoreTracker)
                items = await ReloadEntityInChangeTracker(items, compareProperties);
        }

        public async Task MergeAsync(
            IEnumerable<TModel> items, 
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null, 
            bool ignoreTracker = true)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tempTableName = _bulkStrategy.GetTempTableName($"{nameof(MergeAsync)}_{GetType().Name}_{Guid.NewGuid().ToString().Replace("-", "")}");
                await _bulkStrategy.CreateTempTableAsync(tempTableName);
                await _bulkStrategy.BulkCopyToTempTableAsync(tempTableName, items);
                await _bulkStrategy.MergeTempTableAsync(tempTableName, compareProperties, dontUpdateColumns);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (!ignoreTracker)
                items = await ReloadEntityInChangeTracker(items, compareProperties);
        }

        public async Task SynchronizeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null, 
            bool ignoreTracker = true)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
            //if (_dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
            //    throw new Exception($"{GetType().Name}.{nameof(SynchronizeAsync)} method cannot be executed because it requires a SQL Server provider");

            //await _unitOfWork.BeginTransactionAsync();

            //try
            //{
            //    var tempTableName = $"#{Guid.NewGuid().ToString().Replace("-", "")}_{GetType().Name}_{nameof(SynchronizeAsync)}";
            //    await CreateTempTableAsync(tempTableName);
            //    await BulkCopyToTempTableAsync(tempTableName, items);
            //    await SynchronizeTempTableAsync(tempTableName, compareProperties, dontUpdateColumns);
            //    await _unitOfWork.CommitAsync();
            //}
            //catch (Exception)
            //{
            //    await _unitOfWork.RollbackAsync();
            //    throw;
            //}

            //if (!ignoreTracker)
            //    items = await ReloadEntityInChangeTracker(items, compareProperties);
        }

        public async Task BulkInsertAsync(IEnumerable<TModel> items)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _bulkStrategy.BulkCopyToDbTableAsync(items);
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
            Expression<Func<TModel, object>>? dontUpdateColumns = null,
            bool ignoreTracker = true)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
            //if (_dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
            //    throw new Exception($"{GetType().Name}.{nameof(BulkUpdateAsync)} method cannot be executed because it requires a SQL Server provider");

            //await _unitOfWork.BeginTransactionAsync();

            //try
            //{
            //    var tempTableName = $"#{Guid.NewGuid().ToString().Replace("-", "")}_{GetType().Name}_{nameof(BulkUpdateAsync)}";
            //    await CreateTempTableAsync(tempTableName);
            //    await BulkCopyToTempTableAsync(tempTableName, items);
            //    await MergeUpdateTempTableAsync(tempTableName, compareProperties, dontUpdateColumns);
            //    await _unitOfWork.CommitAsync();
            //}
            //catch (Exception)
            //{
            //    await _unitOfWork.RollbackAsync();
            //    throw;
            //}

            //if (!ignoreTracker)
            //    items = await ReloadEntityInChangeTracker(items, compareProperties);
        }

        protected async Task<IEnumerable<TModel>> ReloadEntityInChangeTracker(IEnumerable<TModel> entities, Expression<Func<TModel, object>> compareProperties)
        {
            // Reload entities
            var predicate = BuildReloadPredicate(entities, compareProperties);
            var compiledPredicate = predicate.Compile();

            // Get all tracked entries in change tracker that could have been modified and detach them
            var trackedEntries = _dbContext.ChangeTracker.Entries<TModel>().Where(e => compiledPredicate(e.Entity));
            foreach (var entry in trackedEntries.ToList())
                entry.State = EntityState.Detached;

            // Reload all entities from database (fresh)
            return _dbContext.Set<TModel>().Where(predicate).AsEnumerable();
        }

        private Expression<Func<TModel, bool>> BuildEqualityExpression(
            PropertyInfo property,
            object? value)
        {
            var parameter = Expression.Parameter(typeof(TModel), "e");
            var propertyAccess = Expression.Property(parameter, property);

            Expression comparison;

            if (value == null)
            {
                // e => e.Property == null
                comparison = Expression.Equal(
                    propertyAccess,
                    Expression.Constant(null, property.PropertyType)
                );
            }
            else
            {
                // e => e.Property == value
                comparison = Expression.Equal(
                    propertyAccess,
                    Expression.Constant(value, property.PropertyType)
                );
            }

            return Expression.Lambda<Func<TModel, bool>>(comparison, parameter);
        }

        /// <summary>
        /// Get the predicate for the where clause that will fetch entities in database based on compareProperties
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        private ExpressionStarter<TModel> BuildReloadPredicate(
            IEnumerable<TModel> entities,
            Expression<Func<TModel, object>> compareProperties
        )
        {
            var predicate = PredicateBuilder.New<TModel>(false); // Start with "false" (OR logic)

            // Extract the properties from the expression
            var properties = _bulkStrategy.GetProperties(compareProperties).ToList();

            foreach (var entity in entities)
            {
                // Build predicate for this specific entity (AND all properties)
                var entityPredicate = PredicateBuilder.New<TModel>(true);

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);

                    // Build: e => e.PropertyName == value
                    var lambda = BuildEqualityExpression(property, value);
                    entityPredicate = entityPredicate.And(lambda);
                }

                // OR this entity's predicate with the overall predicate
                predicate = predicate.Or(entityPredicate);
            }

            return predicate;
        }
    }
}
