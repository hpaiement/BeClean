using System.Linq.Expressions;

namespace BeClean.Repository
{
    public interface IEFBulkRepository<TModel> : IRepository<TModel>
    {
        /// <summary>
        /// Insert items in table if not already exists
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        Task MergeInsertAsync(IEnumerable<TModel> items, Expression<Func<TModel, object>> compareProperties);
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
        /// Insert items in table if not already exists, update the row if exists, delete it if item is no more in source table
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns">List of columns to ignore for update statement</param>
        /// <returns></returns>
        Task SynchronizeAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null);

        /// <summary>
        /// Insert multiple items with high performance
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        Task BulkInsertAsync(IEnumerable<TModel> items);

        /// <summary>
        /// Update items in table in batch if compareProperties matches 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        Task BulkUpdateAsync(
            IEnumerable<TModel> items,
            Expression<Func<TModel, object>> compareProperties,
            Expression<Func<TModel, object>>? dontUpdateColumns = null);
    }
}
