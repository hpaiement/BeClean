using BeClean.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BeClean.DataLayer.UnitOfWork
{
    /// <summary>
    /// Class UnitOfWork
    /// </summary>
    /// <param name="context">The context.</param>
    public class UnitOfWork(DbContext context) : IUnitOfWork
    {
        /// <summary>
        /// The transaction
        /// </summary>
        private IDbContextTransaction? _transaction = null;
        /// <summary>
        /// Stack of string identifier for "nested transactions" savepoints
        /// </summary>
        private readonly Stack<string> _savepointsStack = new();

        /// <inheritdoc/>
        public int TransactionActive
        {
            get
            {
                if (_transaction == null)
                    return 0;
                else
                    return _savepointsStack.Count() + 1;
            }
        }

        /// <inheritdoc/>
        public async Task<string> BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = context.Database.CurrentTransaction ?? await context.Database.BeginTransactionAsync();
                return "root_transaction";
            }
            else
            {
                // Force a save change to be sure no changes cross the savepoint milestone
                await SaveChangesAsync();

                var savepointId = Guid.NewGuid().ToString().Replace("-", "");
                await _transaction.CreateSavepointAsync(savepointId);
                _savepointsStack.Push(savepointId);
                return savepointId;
            }
        }

        /// <inheritdoc/>
        private async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task CommitAsync()
        {
            // Always perform a SaveChanges first to save any work. If this automatic save change is not made here, it
            // makes it confusing to the user because a CommitAsync could potentially not commit every changes made to 
            // the database objects.
            await SaveChangesAsync();

            // Check if a nested transaction was started
            if (_savepointsStack.Count != 0)
            {
                _savepointsStack.Pop();
            }
            else if (_transaction != null)
            {
                await _transaction.CommitAsync();
                _transaction = null;
            }
        }

        /// <inheritdoc/>
        public async Task RollbackAsync(string? transactionId = null)
        {
            if (transactionId != null)
            {
                if (transactionId == "root_transaction")
                    await RollbackAllAsync();
                else if (_savepointsStack.Contains(transactionId))
                {
                    // Force a save change to be sure no changes cross the savepoint milestone
                    await SaveChangesAsync();
                    string lastSavepointId;
                    do
                    {
                        lastSavepointId = _savepointsStack.Pop();
                        await _transaction!.RollbackToSavepointAsync(lastSavepointId);
                    } while (lastSavepointId != transactionId);
                }
            }
            else
            {
                // Check if a nested transaction was started
                if (_savepointsStack.Count != 0)
                {
                    // Force a save change to be sure no changes cross the savepoint milestone
                    await SaveChangesAsync();
                    await _transaction!.RollbackToSavepointAsync(_savepointsStack.Pop());
                }
                else if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    _transaction = null;
                }
            }
        }

        /// <inheritdoc/>
        public async Task RollbackAllAsync()
        {
            while (_transaction != null)
            {
                await RollbackAsync();
            }
        }
    }
}
