using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataContext;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Repositories;
using BeClean.TestLib;
using Microsoft.EntityFrameworkCore;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures
{
    /// <summary>
    /// Creates a new, empty Chinook database (schema only) for the lifetime of the fixture. Data can be seeded
    /// on demand with <see cref="GetOrSeedDataFixture{T}"/>. Provider specific fixtures only have to configure
    /// the database provider.
    /// </summary>
    public abstract class ChinookDbFixture : IDisposable
    {
        private readonly DbContextOptions<ChinookContext> _options;

        private readonly Dictionary<Type, IDataFixture<ChinookContext>> _dataFixtures = new();
        private readonly object _fixturesLock = new();

        /// <param name="connectionStringTemplate">Connection string containing a "&lt;InsertDbNameHere&gt;" placeholder for the database name</param>
        /// <param name="useProvider">Configures the database provider with the resolved connection string</param>
        protected ChinookDbFixture(string connectionStringTemplate, Action<DbContextOptionsBuilder<ChinookContext>, string> useProvider)
        {
            var connectionString = connectionStringTemplate.Replace("<InsertDbNameHere>", $"{DateTime.Now.ToString("yyMMddHHmm")}_{Guid.NewGuid().ToString().Replace("-", "")}");

            var optionsBuilder = new DbContextOptionsBuilder<ChinookContext>();
            useProvider(optionsBuilder, connectionString);
            _options = optionsBuilder.Options;

            using var dbContext = CreateDbContext();
            dbContext.Database.EnsureCreated();
        }

        public ChinookContext CreateDbContext() => new(_options);

        /// <summary>
        /// Returns a new scope (fresh db context, unit of work and repository). Use a separate scope to assert
        /// on data so change tracker state from the "act" part of the test does not leak into assertions.
        /// </summary>
        public ArtistScope CreateArtistScope() => new(CreateDbContext());

        /// <summary>
        /// Returns a lazily-created, shared instance of <typeparamref name="T"/> for the lifetime of this fixture.
        /// The data fixture's <see cref="IDataFixture{TContext}.Seed"/> method is called exactly once, on first access.
        /// </summary>
        public T GetOrSeedDataFixture<T>() where T : IDataFixture<ChinookContext>, new()
        {
            var type = typeof(T);
            if (_dataFixtures.TryGetValue(type, out var existing)) return (T)existing;
            lock (_fixturesLock)
            {
                if (_dataFixtures.TryGetValue(type, out existing)) return (T)existing;
                var fixture = new T();
                using var dbContext = CreateDbContext();
                fixture.Seed(dbContext);
                _dataFixtures[type] = fixture;
                return fixture;
            }
        }

        public void Dispose()
        {
            using var dbContext = CreateDbContext();
            dbContext.Database.EnsureDeleted();
        }
    }

    public sealed class ArtistScope : IDisposable
    {
        public ChinookContext DbContext { get; }
        public ChinookUnitOfWork UnitOfWork { get; }
        public ArtistRepository Repository { get; }

        public ArtistScope(ChinookContext dbContext)
        {
            DbContext = dbContext;
            UnitOfWork = new ChinookUnitOfWork(dbContext);
            Repository = new ArtistRepository(dbContext, UnitOfWork);
        }

        public void Dispose() => DbContext.Dispose();
    }
}
