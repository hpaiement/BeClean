using BeClean.DataLayer.Repositories.Bulk.Strategies;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BeClean.DataLayer.Repositories.Bulk
{
    /// <summary>
    /// EF options extension registering the bulk strategy factory of a database provider in the EF internal service
    /// provider, where <see cref="EFBulkRepository{TModel, TDbContext}"/> resolves it
    /// </summary>
    /// <typeparam name="TStrategyFactory">Database provider specific strategy factory</typeparam>
    public class BulkOptionsExtension<TStrategyFactory> : IDbContextOptionsExtension
        where TStrategyFactory : class, IBulkStatementStrategyFactory
    {
        private DbContextOptionsExtensionInfo? _info;

        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<IBulkStatementStrategyFactory, TStrategyFactory>();
        }

        public void Validate(IDbContextOptions options)
        {
        }

        private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
        {
            public override bool IsDatabaseProvider => false;

            public override string LogFragment => $"using BeClean bulk ({typeof(TStrategyFactory).Name}) ";

            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["BeClean:Bulk:StrategyFactory"] = typeof(TStrategyFactory).Name;
            }
        }
    }
}
