using BeClean.DataLayer.IntegrationTest.Db.Chinook;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.DataFixtures;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using System.Data.Common;

namespace BeClean.DataLayer.IntegrationTest.Db
{
    /// <summary>
    /// Bulk repository tests, run once per supported database provider (see derived classes at the bottom of this file)
    /// </summary>
    public abstract class BulkOperationsTests
    {
        /// <summary>
        /// Creates a new, empty database on the provider under test
        /// </summary>
        protected abstract ChinookDbFixture CreateDbFixture();

        private readonly ArtistBuilder _artistBuilder = new BuilderCollection().GetBuilder<ArtistBuilder>();

        [Fact]
        public async Task MergeAsync_Should_InsertMissingRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            db.GetOrSeedDataFixture<ChinookDataFixture>();
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.MergeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(1000).WithName("NewArtist1").Build(),
                _artistBuilder.WithIdentity(1001).WithName("NewArtist2").Build(),
                _artistBuilder.WithIdentity(1002).WithName("NewArtist3").Build(),
            },
            a => a.ArtistId);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = await assertScope.Repository.GetAllAsync();
            Assert.Equal(3, dbItems.Count(a => a.ArtistId.In(1000, 1001, 1002)));
        }

        [Fact]
        public async Task BulkInsertAsync_Should_InsertAllRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            db.GetOrSeedDataFixture<ChinookDataFixture>();
            using var scope = db.CreateArtistScope();

            int initialCount = (await scope.Repository.GetAllAsync()).Count();

            var artistToInsert = new List<Artist>();
            for (int idx = 0; idx < 10000; idx++)
                artistToInsert.Add(_artistBuilder.WithIdentity(idx + 1000).Build());

            // Act
            await scope.Repository.BulkInsertAsync(artistToInsert);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = await assertScope.Repository.GetAllAsync();
            Assert.Equal(10000 + initialCount, dbItems.Count());
        }

        [Fact]
        public async Task SynchronizeAsync_Should_InsertRowsMissingFromTable()
        {
            // Arrange
            using var db = CreateDbFixture();
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.SynchronizeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(1).WithName("Artist1").Build(),
                _artistBuilder.WithIdentity(2).WithName("Artist2").Build(),
            },
            a => a.ArtistId);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = (await assertScope.Repository.GetAllAsync()).OrderBy(a => a.ArtistId);
            Assert.Collection(dbItems,
                a => { Assert.Equal(1, a.ArtistId); Assert.Equal("Artist1", a.Name); },
                a => { Assert.Equal(2, a.ArtistId); Assert.Equal("Artist2", a.Name); });
        }

        [Fact]
        public async Task SynchronizeAsync_Should_UpdateMatchedRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertArtistsAsync(db, (1, "Artist1"), (2, "Artist2"));
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.SynchronizeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(1).WithName("Artist1Renamed").Build(),
                _artistBuilder.WithIdentity(2).WithName("Artist2Renamed").Build(),
            },
            a => a.ArtistId);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = (await assertScope.Repository.GetAllAsync()).OrderBy(a => a.ArtistId);
            Assert.Collection(dbItems,
                a => { Assert.Equal(1, a.ArtistId); Assert.Equal("Artist1Renamed", a.Name); },
                a => { Assert.Equal(2, a.ArtistId); Assert.Equal("Artist2Renamed", a.Name); });
        }

        [Fact]
        public async Task SynchronizeAsync_Should_DeleteRowsMissingFromItems()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertArtistsAsync(db, (1, "Artist1"), (2, "Artist2"), (3, "Artist3"));
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.SynchronizeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(2).WithName("Artist2").Build(),
            },
            a => a.ArtistId);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = await assertScope.Repository.GetAllAsync();
            Assert.Collection(dbItems,
                a => { Assert.Equal(2, a.ArtistId); Assert.Equal("Artist2", a.Name); });
        }

        [Fact]
        public async Task SynchronizeAsync_Should_NotUpdateDontUpdateColumns()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertArtistsAsync(db, (1, "Artist1"));
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.SynchronizeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(1).WithName("Artist1Renamed").Build(),
                _artistBuilder.WithIdentity(2).WithName("Artist2").Build(),
            },
            a => a.ArtistId,
            dontUpdateColumns: a => a.Name!);

            // Assert
            using var assertScope = db.CreateArtistScope();
            var dbItems = (await assertScope.Repository.GetAllAsync()).OrderBy(a => a.ArtistId);
            Assert.Collection(dbItems,
                a => { Assert.Equal(1, a.ArtistId); Assert.Equal("Artist1", a.Name); },
                a => { Assert.Equal(2, a.ArtistId); Assert.Equal("Artist2", a.Name); });
        }

        [Fact]
        public async Task SynchronizeAsync_WithEmptyItems_Should_DeleteAllRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertArtistsAsync(db, (1, "Artist1"), (2, "Artist2"));
            using var scope = db.CreateArtistScope();

            // Act
            await scope.Repository.SynchronizeAsync(new List<Artist>(), a => a.ArtistId);

            // Assert
            using var assertScope = db.CreateArtistScope();
            Assert.Empty(await assertScope.Repository.GetAllAsync());
        }

        [Fact]
        public async Task SynchronizeAsync_WhenDeleteViolatesForeignKey_Should_ThrowAndRollback()
        {
            // Arrange: Chinook artists are referenced by albums, deleting them violates FK_AlbumArtistId
            using var db = CreateDbFixture();
            db.GetOrSeedDataFixture<ChinookDataFixture>();
            using var scope = db.CreateArtistScope();
            var initialArtists = (await scope.Repository.GetAllAsync()).ToDictionary(a => a.ArtistId, a => a.Name);

            // Act
            var syncTask = scope.Repository.SynchronizeAsync(new List<Artist>
            {
                _artistBuilder.WithIdentity(1).WithName("AC/DC Renamed").Build(),
                _artistBuilder.WithIdentity(1000).WithName("NewArtist").Build(),
            },
            a => a.ArtistId);

            // Assert
            await Assert.ThrowsAnyAsync<DbException>(() => syncTask);

            using var assertScope = db.CreateArtistScope();
            var dbItems = (await assertScope.Repository.GetAllAsync()).ToDictionary(a => a.ArtistId, a => a.Name);
            Assert.Equal(initialArtists, dbItems);
        }

        private async Task InsertArtistsAsync(ChinookDbFixture db, params (int Id, string Name)[] artists)
        {
            using var scope = db.CreateArtistScope();
            await scope.Repository.BulkInsertAsync(artists
                .Select(a => _artistBuilder.WithIdentity(a.Id).WithName(a.Name).Build())
                .ToList());
        }
    }

    public class SqlServerBulkOperationsTests : BulkOperationsTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookSqlServerDbFixture();
    }

    public class PgSqlBulkOperationsTests : BulkOperationsTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookPgSqlDbFixture();
    }
}
