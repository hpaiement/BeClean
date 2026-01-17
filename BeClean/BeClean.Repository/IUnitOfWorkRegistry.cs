namespace BeClean.Repository
{
    /// <summary>
    /// Interface IUnitOfWorkRegistry
    /// </summary>
    public interface IUnitOfWorkRegistry
    {
        /// <summary>
        /// Rollbacks all transactions asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        Task RollbackAllAsync();
    }
}
