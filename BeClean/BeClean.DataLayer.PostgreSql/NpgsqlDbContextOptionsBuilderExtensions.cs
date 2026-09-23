using BeClean.DataLayer.Repositories.Bulk;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace BeClean.DataLayer.PostgreSql
{
    public static class NpgsqlDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Enables BeClean bulk operations (<see cref="EFBulkRepository{TModel, TDbContext}"/>) on PostgreSQL. Example:
        /// <code>options.UseNpgsql(connectionString, o =&gt; o.UseBeCleanBulk());</code>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <returns></returns>
        public static NpgsqlDbContextOptionsBuilder UseBeCleanBulk(this NpgsqlDbContextOptionsBuilder optionsBuilder)
        {
            var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;
            ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(new BulkOptionsExtension<PgBulkStrategyFactory>());

            return optionsBuilder;
        }
    }
}
