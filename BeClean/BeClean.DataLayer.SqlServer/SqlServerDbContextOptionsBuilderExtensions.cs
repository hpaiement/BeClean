using BeClean.DataLayer.Repositories.Bulk;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BeClean.DataLayer.SqlServer
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Enables BeClean bulk operations (<see cref="EFBulkRepository{TModel, TDbContext}"/>) on SQL Server. Example:
        /// <code>options.UseSqlServer(connectionString, o =&gt; o.UseBeCleanBulk());</code>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <returns></returns>
        public static SqlServerDbContextOptionsBuilder UseBeCleanBulk(this SqlServerDbContextOptionsBuilder optionsBuilder)
        {
            var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;
            ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(new BulkOptionsExtension<MsSqlBulkStrategyFactory>());

            return optionsBuilder;
        }
    }
}
