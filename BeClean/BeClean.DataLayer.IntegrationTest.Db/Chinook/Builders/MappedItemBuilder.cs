using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using Bogus;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders
{
    public class MappedItemBuilder : ModelBuilder
    {
        private int? _id = null;
        private string? _label = null;
        private int? _batch = null;

        public MappedItemBuilder WithIdentity(int id)
        {
            _id = id;
            return this;
        }

        public MappedItemBuilder WithLabel(string label)
        {
            _label = label;
            return this;
        }

        public MappedItemBuilder WithBatch(int batch)
        {
            _batch = batch;
            return this;
        }

        /// <summary>
        /// Code is derived from the identity so items can be matched on Code
        /// </summary>
        public MappedItem Build()
        {
            var id = _id ?? GetNextIdentityInt();
            var label = _label;
            var batch = _batch ?? 1;
            var faker = new Faker<MappedItem>()
                .RuleFor(t => t.Id, _ => id)
                .RuleFor(t => t.Code, _ => $"CODE-{id}")
                .RuleFor(t => t.Label, f => label ?? f.Commerce.ProductName())
                .RuleFor(t => t.Position, f => f.Random.Int(0, 1000))
                .RuleFor(t => t.Batch, _ => batch);

            _id = null;
            _label = null;
            _batch = null;

            return faker.Generate();
        }
    }
}
