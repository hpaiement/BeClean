using BeClean.DataLayer.Repositories.Bulk.Strategies;
using BeClean.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        public async Task MergeInsertAsync(IEnumerable<TModel> items, Expression<Func<TModel, object>> compareProperties)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tempTableName = $"{nameof(MergeInsertAsync)}_{GetType().Name}_{Guid.NewGuid().ToString().Replace("-", "")}";
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
        }

        public async Task MergeAsync(
            IEnumerable<TModel> items, 
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tempTableName = $"{nameof(MergeAsync)}_{GetType().Name}_{Guid.NewGuid().ToString().Replace("-", "")}";
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
        }

        public async Task SynchronizeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
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
            Expression<Func<TModel, object>>? dontUpdateColumns = null)
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
        }
    }
}
