using BeClean.DataLayer.IntegrationTest.Db.Chinook;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Repositories;
using System.Diagnostics;

namespace BeClean.DataLayer.IntegrationTest.Db
{
    public class BulkOperationsTest
    {
        public BulkOperationsTest() 
        {
        }

        [Fact]
        public async Task Merge()
        {
            // Arrange
            using var dbContextFixture = new ChinookPgSqlContextFixture();
            var unitOfWork = new ChinookUnitOfWork(dbContextFixture.dbContext);
            var artistRepo = new ArtistRepository(dbContextFixture.dbContext, unitOfWork);
            var artistBuilder = new BuilderCollection().GetBuilder<ArtistBuilder>();

            // Act
            await artistRepo.MergeAsync(new List<Artist>
            {
                artistBuilder
                    .WithIdentity(1000)
                    .WithName("NewArtist1")
                    .Build(),
                artistBuilder
                    .WithIdentity(1001)
                    .WithName("NewArtist2")
                    .Build(),
                artistBuilder
                    .WithIdentity(1002)
                    .WithName("NewArtist3")
                    .Build(),
            },
            a => a.ArtistId);

            // Assert
            var dbItems = await artistRepo.GetAllAsync();
            Assert.Equal(3, dbItems.Count(a => a.ArtistId.In(1000, 1001, 1002)));
        }

        [Fact]
        public async Task BulkInsert()
        {
            // Arrange
            using var dbContextFixture = new ChinookPgSqlContextFixture();
            var unitOfWork = new ChinookUnitOfWork(dbContextFixture.dbContext);
            var artistRepo = new ArtistRepository(dbContextFixture.dbContext, unitOfWork);
            var artistBuilder = new BuilderCollection().GetBuilder<ArtistBuilder>();

            var dbItems = await artistRepo.GetAllAsync();
            int initialCount = dbItems.Count();

            var artistToInsert = new List<Artist>();

            for (int idx = 0; idx < 10000; idx++)
            {
                artistToInsert.Add(artistBuilder
                    .WithIdentity(idx + 1000)
                    .Build());
            }

            // Act
            var perfCounter = new Stopwatch();
            perfCounter.Start();
            await artistRepo.BulkInsertAsync(artistToInsert);
            perfCounter.Stop();


            // Assert
            dbItems = await artistRepo.GetAllAsync();
            Assert.Equal(10000 + initialCount, dbItems.Count());
            Assert.True(perfCounter.Elapsed < TimeSpan.FromMilliseconds(500));
        }
    }
}
