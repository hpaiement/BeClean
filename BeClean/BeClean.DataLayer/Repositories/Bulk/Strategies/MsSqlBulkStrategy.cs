using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace BeClean.DataLayer.Repositories.Bulk.Strategies
{
    public class MsSqlBulkStrategy<TEntity, TDbContext>(
        TDbContext dbContext
    ) : BulkStatementStrategy<TEntity, TDbContext>(dbContext)
        where TDbContext : DbContext
    {
        private const string _providerName = "Microsoft.EntityFrameworkCore.SqlServer";

        public override string GetTempTableName(string baseName) => $"#{baseName}";

        /// <inheritdoc/>
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
                var columnDefinition = $"{columnName} {columnType} {(isNullable ? "NULL" : "NOT NULL")}";
                columnDefinitions.Add(columnDefinition);
            }

            var sql = $@"
CREATE TABLE {tableName} (
    {string.Join(",\n    ", columnDefinitions)}
)";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        /// <inheritdoc/>
        public override async Task BulkCopyToTempTableAsync(string tempTableName, IEnumerable<TEntity> items)
        {
            using (var bulkCopy = _dbContext.Database.CurrentTransaction != null ? new SqlBulkCopy((SqlConnection)_dbContext.Database.GetDbConnection(), SqlBulkCopyOptions.Default, (SqlTransaction)_dbContext.Database.CurrentTransaction.GetDbTransaction()) : new SqlBulkCopy((SqlConnection)_dbContext.Database.GetDbConnection()))
            {
                bulkCopy.DestinationTableName = tempTableName;

                var columns = _entityType
                    .GetProperties();
                    //.Where(p => !(p.ValueGenerated == ValueGenerated.OnAdd && p.IsPrimaryKey()));

                foreach (var column in columns)
                {
                    bulkCopy.ColumnMappings.Add(column.Name, column.Name);
                }

                await bulkCopy.WriteToServerAsync(ToDataTable(items));
            }
        }

        /// <inheritdoc/>
        public override async Task BulkCopyToDbTableAsync(IEnumerable<TEntity> items)
        {
            using (var bulkCopy = _dbContext.Database.CurrentTransaction != null ? new SqlBulkCopy((SqlConnection)_dbContext.Database.GetDbConnection(), SqlBulkCopyOptions.Default, (SqlTransaction)_dbContext.Database.CurrentTransaction.GetDbTransaction()) : new SqlBulkCopy((SqlConnection)_dbContext.Database.GetDbConnection()))
            {
                bulkCopy.DestinationTableName = GetTableFullName();

                var columns = _entityType
                    .GetProperties()
                    .Where(p => !(p.ValueGenerated == ValueGenerated.OnAdd && p.IsPrimaryKey()));

                foreach (var column in columns)
                {
                    bulkCopy.ColumnMappings.Add(column.Name, column.Name);
                }

                await bulkCopy.WriteToServerAsync(ToDataTable(items));
            }
        }

        private DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            var dataTable = new DataTable(typeof(T).Name);

            // Get all the properties of the class
            var properties = _entityType.GetProperties().ToList();
            foreach (var prop in properties)
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType);
            }

            // Populate the DataTable with data
            foreach (var item in data)
            {
                var values = new object?[properties.Count()];
                for (int i = 0; i < properties.Count(); i++)
                {
                    values[i] = properties[i].PropertyInfo!.GetValue(item);
                }
                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        protected override string EncloseDbIdentifier(string identifier)
        {
            return $"[{identifier}]";
        }
    }
}
