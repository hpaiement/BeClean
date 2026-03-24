using BeClean.Repository;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace BeClean.DataLayer.Repositories
{
    public abstract class MemoryReadRepository<TModel> : IReadRepository<TModel>
            where TModel : class
    {
        protected readonly ICollection<TModel> _collection;
        protected readonly PropertyInfo _primaryKeyProperty;

        public MemoryReadRepository()
        {
            _primaryKeyProperty = GetPrimaryKeyProperties()!.First();
            _collection = new List<TModel>();
        }

        public MemoryReadRepository(IEnumerable<TModel> initialCollection)
        {
            _primaryKeyProperty = GetPrimaryKeyProperties()!.First();
            _collection = initialCollection.ToList();
        }

        /// <summary>
        /// Get all as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        public virtual async Task<IEnumerable<TModel>> GetAllAsync() => await Task.FromResult(_collection);

        /// <summary>
        /// Get as an asynchronous operation.
        /// </summary>
        /// <typeparam name="TId">The type of the t identifier.</typeparam>
        /// <param name="id">The id.</param>
        /// <returns>A Task&lt;TModel&gt; representing the asynchronous operation.</returns>
        public virtual async Task<TModel?> GetAsync<TId>(TId id) => await Task.FromResult(_collection.SingleOrDefault(entity => _primaryKeyProperty.GetValue(entity)?.Equals(id) ?? false));

        /// <summary>
        /// Get many entities from dataset, applying filter according to the dictionary of keys
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TModel>> GetManyAsync(Dictionary<string, JsonElement> keys)
        {
            return await Task.FromResult(_collection.Where(GetFilterExpression(keys).Compile()));
        }

        /// <summary>
        /// Gets the primary key properties.
        /// </summary>
        /// <returns>System.Nullable&lt;IReadOnlyList&lt;IProperty&gt;&gt;.</returns>
        public IReadOnlyList<PropertyInfo>? GetPrimaryKeyProperties()
        {
            // Try to find the property with [Key] attribute
            var primaryKeyProperty = typeof(TModel).GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            // If no [Key] attribute is found, fall back to a convention (e.g., "Id" or "ModelNameId")
            if (primaryKeyProperty == null)
            {
                primaryKeyProperty = typeof(TModel).GetProperties()
                    .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                         p.Name.Equals($"{typeof(TModel).Name}Id", StringComparison.OrdinalIgnoreCase));
            }

            if (primaryKeyProperty == null)
            {
                throw new InvalidOperationException($"No primary key found for {typeof(TModel).Name}");
            }

            return new List<PropertyInfo> { primaryKeyProperty };
        }

        public virtual async Task<int> GetTotalRowCountAsync()
        {
            return await Task.FromResult(_collection.Count());
        }

        /// <summary>
        /// Return a binary expression tree taking a TDbModel as parameter. This expression filter the dataset according to the provided key.
        /// The key is a dictionary of field : value to identify which rows should be returned.
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        protected Expression<Func<TModel, bool>> GetFilterExpression(Dictionary<string, JsonElement> keys)
        {
            Expression expression = Expression.Constant(true, typeof(bool)); // Return all by default
            var modelParam = Expression.Parameter(typeof(TModel));
            var modelProperties = typeof(TModel).GetProperties();

            foreach (var keypair in keys)
            {
                var currentProp = modelProperties.Where(prop => prop.Name == keypair.Key).Single();
                var memberExp = Expression.PropertyOrField(modelParam, keypair.Key);

                var deserializeMethod = typeof(JsonSerializer).GetMethod("Deserialize", [typeof(JsonElement), typeof(Type), typeof(JsonSerializerOptions)]);
                //var propCheck = Expression.Equal(memberExp, Expression.Convert(Expression.Constant(keypair.Value), currentProp.PropertyType));
                var propCheck = Expression.Equal(memberExp,
                    Expression.Convert(
                        Expression.Call(
                            deserializeMethod!,
                            Expression.Constant(keypair.Value),
                            Expression.Constant(currentProp.PropertyType),
                            Expression.Constant(JsonSerializerOptions.Default)
                        ),
                        currentProp.PropertyType
                    )
                );
                expression = Expression.AndAlso(expression, propCheck);
            }

            return Expression.Lambda<Func<TModel, bool>>(expression, modelParam);
        }
    }

    public abstract class MemoryRepository<TModel> : MemoryReadRepository<TModel>, IRepository<TModel>
        where TModel : class
    {
        public MemoryRepository() : base()
        {
        }

        public MemoryRepository(IEnumerable<TModel> initialCollection) : base(initialCollection)
        {
        }

        /// <summary>
        /// Inserts the entity. Must use unitOfWork.SaveChangesAsync() or CommitAsync() to confirm operation
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Task&lt;TModel&gt;.</returns>
        public virtual async Task<TModel> InsertAsync(TModel entity)
        {
            var existingEntity = await GetEntityAsync(entity);

            if (existingEntity != null)
                throw new Exception("Cannot insert entity, already exists in storage");

            _collection.Add(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// Insert many as an asynchronous operation.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        public virtual async Task<IEnumerable<TModel>> InsertManyAsync(IEnumerable<TModel> entities)
        {
            foreach(var entity in entities)
                await InsertAsync(entity);

            return entities;
        }

        /// <summary>
        /// Deletes the <see cref="T:System.Threading.Tasks.Task" /> asynchronously.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A Task.</returns>
        public virtual async Task DeleteAsync(TModel entity)
        {
            var existingEntity = await GetEntityAsync(entity);
            if (existingEntity != null)
            {
                _collection.Remove(existingEntity);
            }
        }


        /// <summary>
        /// Updates the <see cref="!:TModel" /> asynchronously.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns><![CDATA[Task<TModel>]]></returns>
        public virtual async Task<TModel> UpdateAsync(TModel entity)
        {
            var existingEntity = await GetEntityAsync(entity);

            // Copy properties from updatedEntity to existingEntity
            foreach (var property in typeof(TModel).GetProperties())
            {
                if (property.CanWrite)
                {
                    var newValue = property.GetValue(entity);
                    property.SetValue(existingEntity, newValue);
                }
            }

            return await Task.FromResult(entity);
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TModel>> UpdatePartialAsync(UpdatePartialDto updateDto, Func<IEnumerable<TModel>, Dictionary<string, JsonElement>, Task>? validateData = null)
        {
            var dbEntities = await GetManyAsync(updateDto.Key);

            if (!dbEntities.Any())
                throw new Exception("No entity to update were found in database");

            // Execute validateData prior to the update. validateData function should throw on validation error.
            if (validateData != null)
                await validateData(dbEntities, updateDto.Values);

            foreach (var dbEntity in dbEntities)
            {
                await SetPropertiesAsync(dbEntity, updateDto.Values);
                await UpdateAsync(dbEntity);
            }

            return dbEntities;
        }

        /// <summary>
        /// Set properties of an entity from the provided dictionary
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected virtual async Task<TModel> SetPropertiesAsync(TModel entity, Dictionary<string, JsonElement> properties)
        {
            var modelProperties = typeof(TModel).GetProperties();

            foreach (var propToUpdate in properties)
            {
                var prop = modelProperties.Where(p => p.Name == propToUpdate.Key).Single();
                var propType = prop.PropertyType;

                if (propType.IsGenericType && propType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    propType = Nullable.GetUnderlyingType(propType);

                // If property is a generic object type, that will cause problem down the path.
                // Hail mary try to convert it to string to avoid exception throw (if that helps)
                if (propType == typeof(object))
                    propType = typeof(string);

                prop.SetValue(entity, propToUpdate.Value.Deserialize(propType!));
            }

            return await Task.FromResult(entity);
        }

        /// <summary>
        /// Delete many as an asynchronous operation - Slow for large dataset
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public virtual async Task DeleteManyAsync(IEnumerable<TModel> entities)
        {
            foreach (var entity in entities)
            {
                await DeleteAsync(entity);
            }
        }

        private async Task<TModel?> GetEntityAsync(TModel dto)
        {
            return await GetAsync(_primaryKeyProperty.GetValue(dto));
        }
    }
}
