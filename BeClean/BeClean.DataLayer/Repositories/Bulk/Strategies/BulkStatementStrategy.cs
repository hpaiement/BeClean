using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    public abstract class BulkStatementStrategy<TEntity, TDbContext> : IBulkStatementStrategy<TEntity, TDbContext>
        where TDbContext : DbContext
    {
        protected readonly TDbContext _dbContext;
        protected readonly IEntityType _entityType;

        /// <param name="dbContext"></param>
        /// <param name="providerName">EF database provider required by the strategy (see DatabaseFacade.ProviderName)</param>
        /// <exception cref="NotSupportedException">The db context uses another database provider</exception>
        protected BulkStatementStrategy(TDbContext dbContext, string providerName)
        {
            if (dbContext.Database.ProviderName != providerName)
                throw new NotSupportedException($"{GetType().Name} requires the {providerName} database provider, but {typeof(TDbContext).Name} uses {dbContext.Database.ProviderName}");

            _dbContext = dbContext;
            _entityType = dbContext.Model.FindEntityType(typeof(TEntity))
                ?? throw new Exception($"{typeof(TEntity).Name} is not a valid model for database {typeof(TDbContext).Name}");
        }

        public abstract Task CreateTempTableAsync(string tableName);

        public abstract Task BulkCopyToTempTableAsync(string tempTableName, IEnumerable<TEntity> items);

        public abstract Task BulkCopyToDbTableAsync(IEnumerable<TEntity> items);

        public virtual async Task MergeTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null
        )
        {
            var onClauseString = GenerateMergeOnClause(compareProperties);
            var insertStatement = GenerateMergeInsertStatement();
            var whenMatchedClause = GenerateMergeWhenMatchedClause(compareProperties, dontUpdateColumns);

            // When matched, update all columns except the compare columns
            var sql =
$@"MERGE INTO {GetTableFullName()} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON
{onClauseString}
{whenMatchedClause}
WHEN NOT MATCHED BY TARGET THEN
{insertStatement};";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public virtual async Task MergeInsertTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties
        )
        {
            var onClauseString = GenerateMergeOnClause(compareProperties);
            var insertStatement = GenerateMergeInsertStatement();

            var sql = 
$@"MERGE INTO {GetTableFullName()} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON 
{onClauseString}
WHEN NOT MATCHED BY TARGET THEN
{insertStatement};";

            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public virtual async Task MergeUpdateTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null
        )
        {
            var whenMatchedClause = GenerateMergeWhenMatchedClause(compareProperties, dontUpdateColumns);

            // Nothing to update (a MERGE statement without WHEN clause is a syntax error)
            if (whenMatchedClause == "")
                return;

            var onClauseString = GenerateMergeOnClause(compareProperties);

            var sql =
$@"MERGE INTO {GetTableFullName()} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON
{onClauseString}
{whenMatchedClause};";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public virtual async Task SynchronizeTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null
        )
        {
            var onClauseString = GenerateMergeOnClause(compareProperties);
            var insertStatement = GenerateMergeInsertStatement();
            var whenMatchedClause = GenerateMergeWhenMatchedClause(compareProperties, dontUpdateColumns);

            // When matched, update all columns except the compare columns
            var sql =
$@"MERGE INTO {GetTableFullName()} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON
{onClauseString}
{whenMatchedClause}
WHEN NOT MATCHED BY TARGET THEN
{insertStatement}
WHEN NOT MATCHED BY SOURCE THEN
DELETE;
";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Returns a string representing the MERGE "ON" clause. Example:
        /// "tgt.CarId = src.CarId AND tgt.CarColorId = src.CarColorId"
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GenerateMergeOnClause(Expression<Func<TEntity, object>> compareProperties)
        {
            var comparePropertyNames = GetPropertyNames(compareProperties);

            var onClauses = new List<string>();

            foreach (var propertyName in comparePropertyNames)
            {
                // Map to the database column name
                var property = _entityType.FindProperty(propertyName);
                if (property == null)
                    throw new InvalidOperationException($"Property '{propertyName}' not found in the entity {typeof(TEntity).Name}.");

                var columnName = GetColumnName(property);

                // Add the comparison to the ON clause
                onClauses.Add($"tgt.{EncloseDbIdentifier(columnName)} = src.{EncloseDbIdentifier(columnName)}");
            }

            return string.Join(" AND ", onClauses);
        }

        /// <summary>
        /// Returns a string containing an INSERT statement with all columns of the table
        /// </summary>
        /// <returns></returns>
        public string GenerateMergeInsertStatement()
        {
            var columnNameToInsert = new List<string>();
            foreach (var property in _entityType.GetProperties())
            {
                if (!(property.ValueGenerated == ValueGenerated.OnAdd && property.IsPrimaryKey()))
                    columnNameToInsert.Add(GetColumnName(property));
            }

            return $"INSERT ({string.Join(",", columnNameToInsert.Select(EncloseDbIdentifier))}) VALUES ({string.Join(",", columnNameToInsert.Select(name => $"src.{EncloseDbIdentifier(name)}"))})";

        }

        /// <summary>
        /// Returns an UPDATE statement for the MERGE statement for all table columns except for those specified in
        /// dontUpdateColumns and those used for merge compare (compareProperties)
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        protected virtual string GenerateMergeUpdateStatement(
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null)
        {
            var columnNameToUpdate = GetColumnNamesToUpdate(compareProperties, dontUpdateColumns);

            return $"UPDATE SET {string.Join(",", columnNameToUpdate.Select(name => $"tgt.{EncloseDbIdentifier(name)}=src.{EncloseDbIdentifier(name)}"))}";
        }

        /// <summary>
        /// Returns the MERGE "WHEN MATCHED THEN UPDATE ..." clause, or an empty string when there is no column left
        /// to update (an empty "UPDATE SET" is a syntax error)
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        public string GenerateMergeWhenMatchedClause(
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null)
        {
            if (!GetColumnNamesToUpdate(compareProperties, dontUpdateColumns).Any())
                return "";

            return $"WHEN MATCHED THEN\n{GenerateMergeUpdateStatement(compareProperties, dontUpdateColumns)}";
        }

        /// <summary>
        /// Returns all table columns except for those specified in dontUpdateColumns, those used for merge compare
        /// (compareProperties) and generated primary keys
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        protected List<string> GetColumnNamesToUpdate(
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null)
        {
            var comparePropertyNames = GetPropertyNames(compareProperties);
            var dontUpdatePropertyNames = GetPropertyNames(dontUpdateColumns);
            var columnNameToUpdate = new List<string>();
            foreach (var property in _entityType.GetProperties())
            {
                if (!(property.ValueGenerated == ValueGenerated.OnAdd && property.IsPrimaryKey()) && !comparePropertyNames.Contains(property.Name) && !dontUpdatePropertyNames.Contains(property.Name))
                    columnNameToUpdate.Add(GetColumnName(property));
            }

            return columnNameToUpdate;
        }

        public virtual string GetTempTableName(string baseName) => baseName;

        public virtual string GetTableFullName()
        {
            // Get the schema and table name
            var schema = _entityType.GetSchema();
            var tableName = EncloseDbIdentifier(_entityType.GetTableName()!);

            return schema != null ? $"{EncloseDbIdentifier(schema)}.{tableName}" : tableName;
        }

        /// <summary>
        /// Returns the table column name of an entity property (may differ from the property name, see HasColumnName)
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected string GetColumnName(IProperty property)
        {
            var columnName = property.GetColumnName(StoreObjectIdentifier.Table(_entityType.GetTableName()!, _entityType.GetSchema()));
            if (string.IsNullOrEmpty(columnName))
                throw new InvalidOperationException($"Column name not found for property '{property.Name}'.");

            return columnName;
        }

        /// <summary>
        /// Returns an enclosed version of a db identifier. Example:
        /// MSSQL : MyIdentifier => [MyIdentifier]
        /// PGSQL : MyIdentifier => "MyIdentifier"
        /// 
        /// To be overriden in specific implementation
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public virtual string EncloseDbIdentifier(string identifier)
        {
            return identifier;
        }

        /// <summary>
        /// Returns a list of table column names from a strong typed property expression
        /// </summary>
        /// <param name="propertyExpression"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected IEnumerable<string> GetPropertyNames(Expression<Func<TEntity, object>>? propertyExpression)
        {
            return GetProperties(propertyExpression).Select(p => p.Name);
        }

        protected IEnumerable<PropertyInfo> GetProperties(Expression<Func<TEntity, object>>? propertyExpression)
        {
            if (propertyExpression == null)
                return Enumerable.Empty<PropertyInfo>();

            IEnumerable<PropertyInfo> compareProperty;
            if (propertyExpression.Body is NewExpression newExpression && newExpression.Members != null)
            {
                // Anonymous object with multiple properties
                //compareProperty = newExpression.Members.Select(m => (PropertyInfo)m).ToList();
                compareProperty = newExpression.Arguments.Select(a => (MemberExpression)a).Select(m => (PropertyInfo)m.Member).ToList();
            }
            else if (propertyExpression.Body is MemberExpression memberExpression)
            {
                // Single property (e.g., x => x.Col1)
                compareProperty = new List<PropertyInfo> { (PropertyInfo)memberExpression.Member };
            }
            else if (
                propertyExpression.Body is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Operand is MemberExpression unaryMemberExpression)
            {
                // Handles boxing for object (e.g., x => (object)x.PrimaryId)
                compareProperty = new List<PropertyInfo> { (PropertyInfo)unaryMemberExpression.Member };
            }
            else
                throw new Exception("compareProperties property format not supported");

            return compareProperty;
        }

    }


}
