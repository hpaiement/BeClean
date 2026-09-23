using System.Linq.Expressions;

namespace BeClean.Repository
{
    /// <summary>
    /// Repository with high performance bulk operations.
    ///
    /// WARNING: bulk operations bypass the change tracker. Entities already tracked by the underlying context are NOT
    /// refreshed and may be stale after a bulk operation: read the resulting data with a new context (or detach / reload
    /// the tracked entities).
    /// </summary>
    public interface IEFBulkRepository<TModel> : IRepository<TModel>
    {
        /// <summary>
        /// Insert items in table if not already exists
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        Task MergeInsertAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties);
        /// <summary>
        /// Insert items in table if not already exists, update the row otherwise
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns">List of columns to ignore for update statement</param>
        /// <returns></returns>
        Task MergeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null);
        /// <summary>
        /// Insert items in table if not already exists, update the row if exists, delete it if item is no more in source table.
        /// Be careful: an empty <paramref name="items"/> collection deletes every row of the table (or of
        /// <paramref name="deleteScope"/>).
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns">List of columns to ignore for update statement</param>
        /// <param name="deleteScope">Only rows matching this predicate are deleted when missing from
        /// <paramref name="items"/> (e.g. x => x.Timestamp >= from). Must be translatable by EF, navigation properties
        /// are supported. Null deletes missing rows of the whole table</param>
        /// <returns></returns>
        Task SynchronizeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null,
            Expression<Func<TModel, bool>>? deleteScope = null);

        /// <summary>
        /// Insert multiple items with high performance. Generated keys (e.g. identity columns) are NOT written back to
        /// <paramref name="items"/>: read the inserted rows from the database if you need them.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        Task BulkInsertAsync(IEnumerable<TModel> items);

        /// <summary>
        /// Update items in table in batch if compareProperties matches
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        Task BulkUpdateAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null);
    }
}
