using BeClean.DataLayer.IntegrationTest.Db.Chinook;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Fixtures;
using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;

namespace BeClean.DataLayer.IntegrationTest.Db
{
    /// <summary>
    /// Bulk repository tests on a table in an explicit schema, with column names different from the property names
    /// (one of them a reserved word). Run once per supported database provider (see derived classes at the bottom of
    /// this file)
    /// </summary>
    public abstract class MappedItemBulkOperationsTests
    {
        /// <summary>
        /// Creates a new, empty database on the provider under test
        /// </summary>
        protected abstract ChinookDbFixture CreateDbFixture();

        private readonly MappedItemBuilder _itemBuilder = new BuilderCollection().GetBuilder<MappedItemBuilder>();

        [Fact]
        public async Task BulkInsertAsync_Should_InsertAllRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            using var scope = db.CreateMappedItemScope();
            var items = new List<MappedItem>
            {
                _itemBuilder.WithIdentity(1).Build(),
                _itemBuilder.WithIdentity(2).Build(),
            };

            // Act
            await scope.Repository.BulkInsertAsync(items);

            // Assert
            await AssertDbItemsAsync(db, items);
        }

        [Fact]
        public async Task MergeInsertAsync_Should_InsertOnlyMissingRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            var existing = _itemBuilder.WithIdentity(1).WithLabel("Existing").Build();
            await InsertItemsAsync(db, existing);
            using var scope = db.CreateMappedItemScope();
            var missing = _itemBuilder.WithIdentity(2).Build();

            // Act
            await scope.Repository.MergeInsertAsync(new List<MappedItem>
            {
                _itemBuilder.WithIdentity(1).WithLabel("Renamed").Build(),
                missing,
            },
            i => i.Code);

            // Assert
            await AssertDbItemsAsync(db, existing, missing);
        }

        [Fact]
        public async Task MergeAsync_Should_InsertMissingRowsAndUpdateMatchedRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertItemsAsync(db, _itemBuilder.WithIdentity(1).WithLabel("Existing").Build());
            using var scope = db.CreateMappedItemScope();
            var items = new List<MappedItem>
            {
                _itemBuilder.WithIdentity(1).WithLabel("Renamed").Build(),
                _itemBuilder.WithIdentity(2).Build(),
            };

            // Act
            await scope.Repository.MergeAsync(items, i => i.Code);

            // Assert
            await AssertDbItemsAsync(db, items);
        }

        [Fact]
        public async Task SynchronizeAsync_Should_InsertUpdateAndDeleteRows()
        {
            // Arrange
            using var db = CreateDbFixture();
            await InsertItemsAsync(db,
                _itemBuilder.WithIdentity(1).WithLabel("Existing").Build(),
                _itemBuilder.WithIdentity(2).Build());
            using var scope = db.CreateMappedItemScope();
            var items = new List<MappedItem>
            {
                _itemBuilder.WithIdentity(1).WithLabel("Renamed").Build(),
                _itemBuilder.WithIdentity(3).Build(),
            };

            // Act
            await scope.Repository.SynchronizeAsync(items, i => i.Code);

            // Assert
            await AssertDbItemsAsync(db, items);
        }

        [Fact]
        public async Task SynchronizeAsync_OnNullableCompareProperty_Should_NotMatchNullValues()
        {
            // Arrange: like the SQL MERGE ON clause, a null compare value never matches (even another null)
            using var db = CreateDbFixture();
            var existing = _itemBuilder.WithIdentity(1).Build();
            existing.Label = null;
            await InsertItemsAsync(db, existing);
            using var scope = db.CreateMappedItemScope();
            var item = _itemBuilder.WithIdentity(2).Build();
            item.Label = null;

            // Act
            await scope.Repository.SynchronizeAsync(new List<MappedItem> { item }, i => i.Label!);

            // Assert
            await AssertDbItemsAsync(db, item);
        }

        [Fact]
        public async Task BulkUpdateAsync_Should_UpdateMatchedRowsExceptDontUpdateColumns()
        {
            // Arrange
            using var db = CreateDbFixture();
            var notInItems = _itemBuilder.WithIdentity(2).Build();
            await InsertItemsAsync(db, _itemBuilder.WithIdentity(1).WithLabel("Existing").WithBatch(1).Build(), notInItems);
            using var scope = db.CreateMappedItemScope();
            var update = _itemBuilder.WithIdentity(1).WithLabel("Renamed").WithBatch(2).Build();

            // Act
            await scope.Repository.BulkUpdateAsync(new List<MappedItem>
            {
                update,
                _itemBuilder.WithIdentity(3).Build(),
            },
            i => i.Code,
            dontUpdateColumns: i => i.Batch);

            // Assert: batch is not updated
            var updated = new MappedItem { Id = 1, Code = update.Code, Label = "Renamed", Position = update.Position, Batch = 1 };
            await AssertDbItemsAsync(db, updated, notInItems);
        }

        [Fact]
        public async Task SynchronizeAsync_WithDeleteScope_Should_OnlyDeleteMissingRowsInScope()
        {
            // Arrange
            using var db = CreateDbFixture();
            var outOfScope = _itemBuilder.WithIdentity(1).WithBatch(1).Build();
            await InsertItemsAsync(db,
                outOfScope,
                _itemBuilder.WithIdentity(2).WithBatch(2).Build(),
                _itemBuilder.WithIdentity(3).WithBatch(2).WithLabel("Existing").Build());
            using var scope = db.CreateMappedItemScope();
            var updated = _itemBuilder.WithIdentity(3).WithBatch(2).WithLabel("Renamed").Build();
            var inserted = _itemBuilder.WithIdentity(4).WithBatch(2).Build();

            // Act
            await scope.Repository.SynchronizeAsync(new List<MappedItem> { updated, inserted }, i => i.Code,
                deleteScope: i => i.Batch >= 2);

            // Assert
            await AssertDbItemsAsync(db, outOfScope, updated, inserted);
        }

        private static async Task InsertItemsAsync(ChinookDbFixture db, params MappedItem[] items)
        {
            using var scope = db.CreateMappedItemScope();
            await scope.Repository.BulkInsertAsync(items);
        }

        private static async Task AssertDbItemsAsync(ChinookDbFixture db, params IEnumerable<MappedItem> expected)
        {
            using var assertScope = db.CreateMappedItemScope();
            var dbItems = (await assertScope.Repository.GetAllAsync()).OrderBy(i => i.Id);
            Assert.Equivalent(expected.OrderBy(i => i.Id), dbItems, strict: true);
        }
    }

    public class SqlServerMappedItemBulkOperationsTests : MappedItemBulkOperationsTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookSqlServerDbFixture();
    }

    public class PgSqlMappedItemBulkOperationsTests : MappedItemBulkOperationsTests
    {
        protected override ChinookDbFixture CreateDbFixture() => new ChinookPgSqlDbFixture();
    }
}
