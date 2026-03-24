namespace BeClean.Repository
{
    /// <summary>
    /// Interface IUnitOfWork
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Begins the transaction asynchronous.
        /// </summary>
        /// <returns>The transaction ID</returns>
        Task<string> BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction scope
        /// </summary>
        /// <returns>Task.</returns>
        Task CommitAsync();

        /// <summary>
        /// Rollback the current transaction scope
        /// </summary>
        /// <param name="transactionId">
        /// The transaction id (returned by BeginTransactionAsync) to which the rollback must occur.
        /// Every savepoints created after this one will be rolled back, including the specified one.
        /// </param>
        /// <returns></returns>
        Task RollbackAsync(string? transactionId = null);

        /// <summary>
        /// Rollback all transactions (nested up to outer one)
        /// </summary>
        /// <returns>Task.</returns>
        Task RollbackAllAsync();
        /// <summary>
        /// Return the number of transaction (savepoints) active
        /// </summary>
        int TransactionActive { get; }
    }
}
