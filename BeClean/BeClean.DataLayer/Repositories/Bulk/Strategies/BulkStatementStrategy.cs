using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    public abstract class BulkStatementStrategy<TEntity, TDbContext>(
        TDbContext dbContext
    ) : IBulkStatementStrategy<TEntity, TDbContext>
        where TDbContext : DbContext
    {
        protected readonly TDbContext _dbContext = dbContext;
        protected readonly IEntityType _entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new Exception($"{nameof(TEntity)} is not a valid model for database {nameof(TDbContext)}");

        public virtual async Task CreateTempTableAsync(string tableName)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task BulkCopyToTempTableAsync(string tempTableName, IEnumerable<TEntity> items)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task BulkCopyToDbTableAsync(IEnumerable<TEntity> items)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public virtual async Task MergeTempTableAsync(
            string tempTableName,
            Expression<Func<TEntity, object>> compareProperties,
            Expression<Func<TEntity, object>>? dontUpdateColumns = null
        )
        {
            var onClauseString = GenerateMergeOnClause(compareProperties);
            var insertStatement = GenerateMergeInsertStatement();
            var updateStatement = GenerateMergeUpdateStatement(compareProperties, dontUpdateColumns);

            // When matched, update all columns except the compare columns
            var sql =
$@"MERGE INTO {EncloseDbIdentifier(GetTableFullName())} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON 
{onClauseString}
WHEN MATCHED THEN
{updateStatement}
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
$@"MERGE INTO {EncloseDbIdentifier(GetTableFullName())} tgt
USING {EncloseDbIdentifier(tempTableName)} src ON 
{onClauseString}
WHEN NOT MATCHED BY TARGET THEN
{insertStatement};";

            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Returns a string representing the MERGE "ON" clause. Example:
        /// "tgt.CarId = src.CarId AND tgt.CarColorId = src.CarColorId"
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected string GenerateMergeOnClause(Expression<Func<TEntity, object>> compareProperties)
        {
            var comparePropertyNames = GetPropertyNames(compareProperties);

            var onClauses = new List<string>();

            foreach (var propertyName in comparePropertyNames)
            {
                // Map to the database column name
                var property = _entityType.FindProperty(propertyName);
                if (property == null)
                    throw new InvalidOperationException($"Property '{propertyName}' not found in the entity {typeof(TEntity).Name}.");

                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(_entityType.GetTableName()!, _entityType.GetSchema()));
                if (string.IsNullOrEmpty(columnName))
                    throw new InvalidOperationException($"Column name not found for property '{propertyName}'.");

                // Add the comparison to the ON clause
                onClauses.Add($"tgt.{EncloseDbIdentifier(columnName)} = src.{EncloseDbIdentifier(columnName)}");
            }

            return string.Join(" AND ", onClauses);
        }

        /// <summary>
        /// Returns a string containing an INSERT statement with all columns of the table
        /// </summary>
        /// <returns></returns>
        protected string GenerateMergeInsertStatement()
        {
            var columnNameToInsert = new List<string>();
            foreach (var property in _entityType.GetProperties())
            {
                if (!(property.ValueGenerated == ValueGenerated.OnAdd && property.IsPrimaryKey()))
                    columnNameToInsert.Add(property.Name);
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
            var comparePropertyNames = GetPropertyNames(compareProperties);
            var dontUpdatePropertyNames = GetPropertyNames(dontUpdateColumns);
            var columnNameToUpdate = new List<string>();
            foreach (var property in _entityType.GetProperties())
            {
                if (!(property.ValueGenerated == ValueGenerated.OnAdd && property.IsPrimaryKey()) && !comparePropertyNames.Contains(property.Name) && !dontUpdatePropertyNames.Contains(property.Name))
                    columnNameToUpdate.Add(property.Name);
            }

            return $"UPDATE SET {string.Join(",", columnNameToUpdate.Select(name => $"tgt.{EncloseDbIdentifier(name)}=src.{EncloseDbIdentifier(name)}"))}";
        }

        public virtual string GetTempTableName(string baseName) => baseName;

        public virtual string GetTableFullName()
        {
            // Get the schema and table name
            var schema = _entityType.GetSchema();
            var tableName = _entityType.GetTableName();

            return schema != null ? $"{schema}.{tableName!}" : $"{tableName!}";
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
        protected virtual string EncloseDbIdentifier(string identifier)
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
            if (propertyExpression == null)
                return Enumerable.Empty<string>();

            IEnumerable<string> comparePropertyNames;
            if (propertyExpression.Body is NewExpression newExpression && newExpression.Members != null)
            {
                // Anonymous object with multiple properties
                comparePropertyNames = newExpression.Members.Select(m => m.Name).ToList();
            }
            else if (propertyExpression.Body is MemberExpression memberExpression)
            {
                // Single property (e.g., x => x.Col1)
                comparePropertyNames = new List<string> { memberExpression.Member.Name };
            }
            else if (
                propertyExpression.Body is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Operand is MemberExpression unaryMemberExpression)
            {
                // Handles boxing for object (e.g., x => (object)x.PrimaryId)
                comparePropertyNames = new List<string> { unaryMemberExpression.Member.Name };
            }
            else
                throw new Exception("compareProperties property format not supported");

            return comparePropertyNames;
        }

    }
}
