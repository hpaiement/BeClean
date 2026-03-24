using BeClean.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace BeClean.DataLayer.Repositories
{
    public abstract class EFReadRepository<TModel, TDbContext> : IReadRepository<TModel>
            where TModel : class
            where TDbContext : DbContext
    {
        /// <summary>
        /// The database context
        /// </summary>
        protected readonly TDbContext _dbContext;

        /// <summary>
        /// The database table
        /// </summary>
        protected readonly DbSet<TModel> _dbSet;

        public EFReadRepository(
            TDbContext dbContext
        )
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<TModel>();
        }


        /// <summary>
        /// Get all as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        public virtual async Task<IEnumerable<TModel>> GetAllAsync() => await _dbSet.ToListAsync();

        /// <summary>
        /// Get as an asynchronous operation.
        /// </summary>
        /// <typeparam name="TId">The type of the t identifier.</typeparam>
        /// <param name="id">The id.</param>
        /// <returns>A Task&lt;TModel&gt; representing the asynchronous operation.</returns>
        public virtual async Task<TModel?> GetAsync<TId>(TId id) => await _dbSet.FindAsync(id);

        /// <summary>
        /// Get many entities from dataset, applying filter according to the dictionary of keys
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<TModel>> GetManyAsync(Dictionary<string, JsonElement> keys)
        {
            IQueryable<TModel> query = _dbSet;

            query = query.Where(GetFilterExpression(keys));

            return await query.ToListAsync();
        }

        /// <summary>
        /// Gets the primary key properties.
        /// </summary>
        /// <returns>System.Nullable&lt;IReadOnlyList&lt;IProperty&gt;&gt;.</returns>
        public IReadOnlyList<PropertyInfo>? GetPrimaryKeyProperties()
        {
            IEntityType? entityType = _dbContext.Model.FindEntityType(typeof(TModel));
            IKey? primaryKey = entityType?.FindPrimaryKey();

            return primaryKey?.Properties
                .Select(p => p.PropertyInfo!)
                .Where(p => p != null) // Ensure non-null PropertyInfo
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc/>
        public virtual async Task<int> GetTotalRowCountAsync()
        {
            return await _dbSet.CountAsync();
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

    public abstract class EFRepository<TModel, TDbContext>(
        TDbContext dbContext
    ) : EFReadRepository<TModel, TDbContext>(dbContext), IRepository<TModel>
        where TModel : class
        where TDbContext : DbContext
    {

        /// <summary>
        /// Inserts the entity. Must use unitOfWork.SaveChangesAsync() or CommitAsync() to confirm operation
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Task&lt;TModel&gt;.</returns>
        public virtual async Task<TModel> InsertAsync(TModel entity)
        {
            _dbSet.Add(entity);
            //_dbContext.Entry(dbEntity).State = EntityState.Added;
            await _dbContext.SaveChangesAsync();

            // Prevent EF Core tracking to be able to use decoupled repository
            //_dbContext.Entry(dbEntity).State = EntityState.Detached;
            //DetachAllEntities();

            return entity;
        }

        /// <summary>
        /// Insert many as an asynchronous operation.
        /// </summary>
        /// <param name="entities">The entities.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        public virtual async Task<IEnumerable<TModel>> InsertManyAsync(IEnumerable<TModel> entities)
        {
            //foreach(var dbEntity in dbEntities)
            //    _dbContext.Entry(dbEntity).State = EntityState.Added;
            _dbSet.AddRange(entities);
            await _dbContext.SaveChangesAsync();

            // Prevent EF Core tracking to be able to use decoupled repository
            //foreach(var entity in dbEntities)
            //    _dbContext.Entry(entity).State = EntityState.Detached;
            //DetachAllEntities();

            return entities;
        }

        /// <summary>
        /// Deletes the <see cref="T:System.Threading.Tasks.Task" /> asynchronously.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A Task.</returns>
        public virtual async Task DeleteAsync(TModel entity)
        {
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync();
 

            //var dbEntity = _mapper.Map<TDbModel>(entity);
            //_dbSet.Remove(dbEntity);
            //await _dbContext.SaveChangesAsync();
            //DetachAllEntities();
        }


        /// <summary>
        /// Updates the <see cref="!:TModel" /> asynchronously.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns><![CDATA[Task<TModel>]]></returns>
        public virtual async Task<TModel> UpdateAsync(TModel entity)
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
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
            _dbSet.RemoveRange(entities);
            await _dbContext.SaveChangesAsync();

            //var dbEntities = _mapper.Map<IEnumerable<TDbModel>>(entities);
            //_dbSet.RemoveRange(dbEntities);
            //await _dbContext.SaveChangesAsync();

            //DetachAllEntities();
        }

        private async Task<TModel> GetEntityAsync(TModel dto)
        {
            var entityType = _dbContext.Model.FindEntityType(typeof(TModel));
            if (entityType == null)
                throw new InvalidOperationException($"Entity type {typeof(TModel).Name} not found in DbContext.");

            var keyProperties = entityType.FindPrimaryKey()?.Properties;
            if (keyProperties == null || keyProperties.Count == 0)
                throw new InvalidOperationException($"Entity {typeof(TModel).Name} has no primary key defined.");

            var keyValues = keyProperties.Select(p => typeof(TModel).GetProperty(p.Name)?.GetValue(dto)).ToArray();

            // Otherwise, try to retrieve from the database
            return await _dbSet.FindAsync(keyValues) ?? throw new Exception($"Entity does not exist in database ({entityType.Name} : {keyValues})");
        }
    }
}
