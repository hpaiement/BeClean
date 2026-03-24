using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using Bogus;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders
{
    public class ArtistBuilder : ModelBuilder
    {
        private int? _artistId = null;
        private string? _name = null;

        public ArtistBuilder WithIdentity(int id)
        {
            _artistId = id;
            return this;
        }

        public ArtistBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public Artist Build()
        {
            var faker = new Faker<Artist>()
                .RuleFor(t => t.ArtistId, _ => _artistId ?? GetNextIdentityInt())
                .RuleFor(t => t.Name, (f, m) => _name ?? f.Name.FirstName());

            return faker.Generate();
        }
    }
}
