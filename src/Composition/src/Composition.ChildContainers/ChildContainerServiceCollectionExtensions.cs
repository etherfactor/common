using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ChildContainerServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        /// <summary>
        /// Gets the next child container id.
        /// </summary>
        /// <returns>The next child container id.</returns>
        private static Guid GetChildContainerId()
        {
            var childContainerId = Guid.NewGuid();
            return childContainerId;
        }

        /// <summary>
        /// Adds a child container to the current service collection. Child containers exist on their own, with their own
        /// services. Services from the parent container can be imported into the child container, and services added to the
        /// child container can be forwarded back to the parent container.
        /// </summary>
        /// <param name="this">Itself.</param>
        /// <param name="configureChild">The child configuration.</param>
        /// <returns>Itself.</returns>
        public IChildContainerBuilder AddChildContainer(
            Action<IServiceCollection, IServiceProvider> configureChild)
        {
            //Add the factory that can produce child containers
            @this.TryAddSingleton<ChildServiceProviderFactory>();

            //Produce the next child container id
            var childContainerId = GetChildContainerId();

            //Construct a builder for the child container
            var builder = new ChildContainerBuilder(childContainerId, @this, configureChild);

            return builder;
        }
    }
}
