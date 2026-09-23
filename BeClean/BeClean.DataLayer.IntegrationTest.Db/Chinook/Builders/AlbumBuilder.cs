using BeClean.DataLayer.IntegrationTest.Db.Chinook.Models;
using Bogus;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders
{
    public class AlbumBuilder : ModelBuilder
    {
        private int? _albumId = null;
        private string? _title = null;
        private int? _artistId = null;

        public AlbumBuilder WithIdentity(int id)
        {
            _albumId = id;
            return this;
        }

        public AlbumBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public AlbumBuilder WithArtist(int artistId)
        {
            _artistId = artistId;
            return this;
        }

        public Album Build()
        {
            var albumId = _albumId ?? GetNextIdentityInt();
            var title = _title;
            var artistId = _artistId ?? 1;
            var faker = new Faker<Album>()
                .RuleFor(t => t.AlbumId, _ => albumId)
                .RuleFor(t => t.Title, f => title ?? f.Commerce.ProductName())
                .RuleFor(t => t.ArtistId, _ => artistId);

            _albumId = null;
            _title = null;
            _artistId = null;

            return faker.Generate();
        }
    }
}
