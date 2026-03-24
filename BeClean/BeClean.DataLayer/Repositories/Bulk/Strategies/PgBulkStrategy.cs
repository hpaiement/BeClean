using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    public class PgBulkStrategy<TEntity, TDbContext>(
        TDbContext dbContext
    ) : BulkStatementStrategy<TEntity, TDbContext>(dbContext)
        where TDbContext : DbContext
    {
        private const string _providerName = "Npgsql.EntityFrameworkCore.PostgreSQL";

        public override async Task CreateTempTableAsync(string tableName)
        {
            if (_dbContext.Database.ProviderName != _providerName)
                throw new Exception($"{GetType().Name}.{nameof(CreateTempTableAsync)} method cannot be executed because it requires a SQL Server provider");

            var columns = _entityType.GetProperties();

            var columnDefinitions = new List<string>();

            foreach (var column in columns)
            {
                var columnName = column.GetColumnName(); // Get column name
                var columnType = column.GetColumnType(); // Get SQL type
                var isNullable = column.IsNullable;      // Check nullability

                // Build the column definition
                var columnDefinition = $"{EncloseDbIdentifier(columnName)} {columnType} {(isNullable ? "NULL" : "NOT NULL")}";
                columnDefinitions.Add(columnDefinition);
            }

            var sql = $@"
CREATE TEMPORARY TABLE {EncloseDbIdentifier(tableName)} (
    {string.Join(",\n    ", columnDefinitions)}
)";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <inheritdoc/>
        public override async Task BulkCopyToTempTableAsync(string tableName, IEnumerable<TEntity> items)
        {
            if (_dbContext.Database.ProviderName != _providerName)
                throw new Exception($"{GetType().Name}.{nameof(CreateTempTableAsync)} method cannot be executed because it requires a {_providerName} provider");

            var dbConnection = (Npgsql.NpgsqlConnection)_dbContext.Database.GetDbConnection();

            var copyStatement = $"COPY {EncloseDbIdentifier(tableName)} ({string.Join(",", _entityType.GetProperties().Select(p => EncloseDbIdentifier(p.Name)))}) FROM STDIN (FORMAT BINARY)";

            using (var writer = await dbConnection.BeginBinaryImportAsync(copyStatement))
            {
                foreach (var item in items)
                {
                    await writer.WriteRowAsync(default, _entityType
                        .GetProperties()
                        //.Where(p => !(p.ValueGenerated == ValueGenerated.OnAdd && p.IsPrimaryKey()))
                        .Select(p => p.PropertyInfo!.GetValue(item))
                        .ToArray());
                }

                await writer.CompleteAsync();
            }
        }

        /// <inheritdoc/>
        public override async Task BulkCopyToDbTableAsync(IEnumerable<TEntity> items)
        {
            if (_dbContext.Database.ProviderName != _providerName)
                throw new Exception($"{GetType().Name}.{nameof(CreateTempTableAsync)} method cannot be executed because it requires a {_providerName} provider");

            var dbConnection = (Npgsql.NpgsqlConnection)_dbContext.Database.GetDbConnection();
            var entityPropertiesToCopy = _entityType
                .GetProperties()
                .Where(p => !(p.ValueGenerated == ValueGenerated.OnAdd && p.IsPrimaryKey()));

            var copyStatement = $"COPY {EncloseDbIdentifier(GetTableFullName())} ({string.Join(",", entityPropertiesToCopy.Select(p => EncloseDbIdentifier(p.Name)))}) FROM STDIN (FORMAT BINARY)";

            using (var writer = await dbConnection.BeginBinaryImportAsync(copyStatement))
            {
                foreach (var item in items)
                {
                    await writer.WriteRowAsync(default, entityPropertiesToCopy
                        .Select(p => p.PropertyInfo!.GetValue(item))
                        .ToArray());
                }

                await writer.CompleteAsync();
            }
        }

        protected override string EncloseDbIdentifier(string identifier)
        {
            return $"\"{identifier}\"";
        }

        /// <summary>
        /// Override because pgsql requires no table alias for the property name being updated
        /// </summary>
        /// <param name="compareProperties"></param>
        /// <param name="dontUpdateColumns"></param>
        /// <returns></returns>
        protected override string GenerateMergeUpdateStatement(
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

            return $"UPDATE SET {string.Join(",", columnNameToUpdate.Select(name => $"{EncloseDbIdentifier(name)}=src.{EncloseDbIdentifier(name)}"))}";
        }

        public override async Task MergeTempTableAsync(
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
WHEN NOT MATCHED THEN
{insertStatement};";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public override async Task MergeInsertTempTableAsync(
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
WHEN NOT MATCHED THEN
{insertStatement};";

            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
