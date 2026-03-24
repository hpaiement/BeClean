using BeClean.DataLayer.IntegrationTest.Db.Chinook.Builders;
using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace BeClean.DataLayer.IntegrationTest.Db.Chinook
{
    public class BuilderCollection
    {
        private ServiceProvider _serviceProvider;

        public BuilderCollection() 
        {
            Randomizer.Seed = new Random();

            var servicesCollection = new ServiceCollection();

            servicesCollection.AddSingleton<ArtistBuilder>();

            _serviceProvider = servicesCollection.BuildServiceProvider();
        }

        public TBuilder GetBuilder<TBuilder>()
            where TBuilder : notnull
        {
            return _serviceProvider.GetRequiredService<TBuilder>();
        }
    }
}
