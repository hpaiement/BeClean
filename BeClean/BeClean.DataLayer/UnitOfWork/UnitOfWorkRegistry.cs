using BeClean.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeClean.DataLayer.UnitOfWork
{
    /// <summary>
    /// Class UnitOfWorkRegistry.
    /// </summary>
    public class UnitOfWorkRegistry : IUnitOfWorkRegistry
    {
        /// <summary>
        /// The service provider
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// The service collection.
        /// </summary>
        private readonly IServiceCollection _serviceCollection;

        /// <summary>
        /// The Unit Of Work collection
        /// </summary>
        private readonly List<IUnitOfWork> _uowCollection = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkRegistry"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="serviceCollectionProvider">The service collection provider.</param>

        public UnitOfWorkRegistry(IServiceProvider serviceProvider, IServiceCollectionProvider serviceCollectionProvider)
        {
            _serviceProvider = serviceProvider;
            _serviceCollection = serviceCollectionProvider.ServiceCollection;

            var uowServices = _serviceCollection
                //.Where(s => s.ServiceType.FullName != null && s.ServiceType.FullName.Contains("IUnitOfWork") && s.ServiceType.IsGenericType)
                //.Where(s => s.ServiceType == typeof(IUnitOfWork))
                .Where(s => s.ServiceType.GetInterfaces().Contains(typeof(IUnitOfWork)))
                .ToList();

            foreach (var serviceDesc in uowServices)
                _uowCollection.AddRange((IEnumerable<IUnitOfWork>)_serviceProvider.GetServices(serviceDesc.ServiceType));
        }

        /// <summary>
        /// Rollback all transactions as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task RollbackAllAsync()
        {
            foreach (var uow in _uowCollection)
                await uow.RollbackAllAsync();
        }
    }

    /// <summary>
    /// Class UnitOfWorkRegistryExtensions.
    /// </summary>
    public static class UnitOfWorkRegistryExtensions
    {
        /// <summary>
        /// Adds the unit of work in the service collection.
        /// </summary>
        /// <typeparam name="TInterface">Unit of work interface</typeparam>
        /// <typeparam name="TUnitOfWork">Unit of work type</typeparam>
        /// <param name="services">The services.</param>
        /// <returns>IServiceCollection.</returns>
        public static IServiceCollection AddUnitOfWork<TInterface, TUnitOfWork>(this IServiceCollection services)
            where TInterface : class, IUnitOfWork
            where TUnitOfWork : class, TInterface
        {
            services.TryAddSingleton<IServiceCollectionProvider>(new ServiceCollectionProvider(services));
            services.TryAddScoped<IUnitOfWorkRegistry, UnitOfWorkRegistry>();
            services.AddScoped<TInterface, TUnitOfWork>();
            return services;
        }
    }

    /// <summary>
    /// Interface IServiceCollectionProvider
    /// </summary>
    public interface IServiceCollectionProvider
    {
        /// <summary>
        /// Gets the service collection.
        /// </summary>
        /// <value>The service collection.</value>
        IServiceCollection ServiceCollection { get; }
    }

    /// <summary>
    /// Class ServiceCollectionProvider. This class cannot be inherited.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public sealed class ServiceCollectionProvider(IServiceCollection serviceCollection) : IServiceCollectionProvider
    {

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        /// <value>The service collection.</value>
        public IServiceCollection ServiceCollection => serviceCollection;
    }
}
