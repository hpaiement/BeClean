using System.Reflection;
using System.Text.Json;

namespace BeClean.Repository
{

    /// <summary>
    /// Interface IReadGenericRepository
    /// </summary>
    /// <typeparam name="TModel">The type of the t model.</typeparam>
    public interface IReadRepository<TModel>
    {
        /// <summary>
        /// Gets the entity asynchronous.
        /// </summary>
        /// <typeparam name="TId">The type of the t identifier.</typeparam>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;System.Nullable&lt;TModel&gt;&gt;.</returns>
        Task<TModel?> GetAsync<TId>(TId id);
        /// <summary>
        /// Gets all entities asynchronous.
        /// </summary>
        /// <returns>Task&lt;IEnumerable&lt;TModel&gt;&gt;.</returns>
        Task<IEnumerable<TModel>> GetAllAsync();
        /// <summary>
        /// Get many entities from dataset, applying filter according to the dictionary of keys
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        Task<IEnumerable<TModel>> GetManyAsync(Dictionary<string, JsonElement> keys);
        /// <summary>
        /// Gets the primary key properties.
        /// </summary>
        /// <returns>System.Nullable&lt;IReadOnlyList&lt;IProperty&gt;&gt;.</returns>
        IReadOnlyList<PropertyInfo>? GetPrimaryKeyProperties();
        /// <summary>
        /// Get the total number of rows in a table
        /// </summary>
        /// <returns></returns>
        Task<int> GetTotalRowCountAsync();
    }

    /// <summary>
    /// Interface IWriteGenericRepository
    /// </summary>
    /// <typeparam name="TModel">The type of the t model.</typeparam>
    public interface IWriteRepository<TModel>
    {
        /// <summary>
        /// Inserts the entity asynchronous.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Task&lt;TModel&gt;.</returns>
        Task<TModel> InsertAsync(TModel entity);
        /// <summary>
        /// Inserts many entities asynchronous.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>Task&lt;IEnumerable&lt;TModel&gt;&gt;.</returns>
        Task<IEnumerable<TModel>> InsertManyAsync(IEnumerable<TModel> entities);

        /// <summary>
        /// Updates the entity asynchronous.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Task&lt;TModel&gt;.</returns>
        Task<TModel> UpdateAsync(TModel entity);

        /// <summary>
        /// Update partially with given updateDto.Values one or a group of entity, selected by updateDto.Key
        /// </summary>
        /// <param name="updateDto"></param>
        /// <param name="validateData">Optional data validation function. This validation function must take the model entity prior to update
        /// and the dictionary of values that will be used for update. The function is expected to throw if validation fails.
        /// </param>
        /// <returns></returns>
        Task<IEnumerable<TModel>> UpdatePartialAsync(UpdatePartialDto updateDto, Func<IEnumerable<TModel>, Dictionary<string, JsonElement>, Task>? validateData = null);

        /// <summary>
        /// Deletes the entity asynchronous.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Task.</returns>
        Task DeleteAsync(TModel entity);
    }

    /// <summary>
    /// Interface IGenericRepository
    /// Extends the <see cref="IReadRepository{TModel}" />
    /// Extends the <see cref="IWriteRepository{TModel}" />
    /// </summary>
    /// <typeparam name="TModel">The type of the t model.</typeparam>
    /// <seealso cref="IReadRepository{TModel}" />
    /// <seealso cref="IWriteRepository{TModel}" />
    public interface IRepository<TModel> : IReadRepository<TModel>, IWriteRepository<TModel> { }

}
