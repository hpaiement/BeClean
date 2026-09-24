using BeClean.DataLayer.IntegrationTest.Db.Chinook;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;

namespace BeClean.DataLayer.IntegrationTest.Db
{
    /// <summary>
    /// Read repository tests, run once per supported database provider (see derived classes at the bottom of this file)
    /// </summary>
    public abstract class ReadRepositoryTests
    {
        /// <summary>
        /// Creates a new, empty database on the provider under test
        /// </summary>
        protected abstract ChinookDbFixture CreateDbFixture();

        private readonly ArtistBuilder _artistBuilder = new BuilderCollection().GetBuilder<ArtistBuilder>();

        [Fact]
        public async Task GetNoTrackingAsync_Should_ReturnDatabaseValues_When_TrackedInstanceHasUnsavedChanges()
        {
            // Arrange
            using var db = CreateDbFixture();
            using (var seedScope = db.CreateArtistScope())
                await seedScope.Repository.InsertAsync(_artistBuilder.WithIdentity(1).WithName("Original").Build());

            using var scope = db.CreateArtistScope();
            var trackedArtist = await scope.Repository.GetAsync(1);
            trackedArtist!.Name = "Modified";

            // Act
            var dbArtist = await scope.Repository.GetNoTrackingAsync(1);

            // Assert
            Assert.NotNull(dbArtist);
            Assert.NotSame(trackedArtist, dbArtist);
            Assert.Equal("Original", dbArtist.Name);
        }

        [Fact]
        public async Task GetNoTrackingAsync_Should_ReturnNull_When_IdDoesNotExist()
        {
            // Arrange
            using var db = CreateDbFixture();
            using var scope = db.CreateArtistScope();

            // Act
            Artist? dbArtist = await scope.Repository.GetNoTrackingAsync(12345);

            // Assert
            Assert.Null(dbArtist);
        }
    }

    public class SqlServerReadRepositoryTests : ReadRepositoryTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookSqlServerDbFixture();
    }

    public class PgSqlReadRepositoryTests : ReadRepositoryTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookPgSqlDbFixture();
    }
}
