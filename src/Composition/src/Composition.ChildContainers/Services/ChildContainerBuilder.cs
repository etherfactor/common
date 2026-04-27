using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

/// <summary>
/// Provides methods to pass services back and forth between a parent and a child service container. Does not initialize any scopes.
/// </summary>
internal class ChildContainerBuilder : IChildContainerBuilder
{
    private readonly Guid _childContainerId;

    private readonly IServiceCollection _parentServices;
    private readonly IServiceCollection _childServices;
    private readonly Action<IServiceCollection, IServiceProvider> _configureChild;
    private readonly List<(Type ServiceType, ServiceLifetime Lifetime)> _childImports = new();

    /// <summary>
    /// Keep track of dependency chains on a per-thread basis. If we end up back in this container, resolving the same
    /// type, there's a dependency chain, and the user needs to be notified.
    /// </summary>
    private static readonly ThreadLocal<HashSet<ValueTuple<ChildContainerBuilder, Type>>> _resolutionStack = new(() => new());

    /// <inheritdoc/>
    public IServiceCollection ChildServices => _childServices;

    public ChildContainerBuilder(
        Guid childContainerId,
        IServiceCollection parentServices,
        Action<IServiceCollection, IServiceProvider> configureChild)
    {
        _childContainerId = childContainerId;
        _parentServices = parentServices;
        _childServices = new ServiceCollection();
        _configureChild = configureChild;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ForwardScoped<TService>()
        where TService : class
    {
        _parentServices.AddScoped(services =>
        {
            AssertNoCycle<TService>();
            AddToStack<TService>();

            try
            {
                var factory = services.GetRequiredService<ChildServiceProviderFactory>();

                factory.TryAddServiceCollection(_childContainerId, _childServices, _configureChild, _childImports);
                var thisServiceProvider = factory.GetScopedServiceProvider(_childContainerId, services);

                var service = thisServiceProvider.GetRequiredService<TService>();

                return service;
            }
            finally
            {
                RemoveFromStack<TService>();
            }
        });

        return this;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ForwardSingleton<TService>()
        where TService : class
    {
        _parentServices.AddSingleton(services =>
        {
            AssertNoCycle<TService>();
            AddToStack<TService>();

            try
            {
                var factory = services.GetRequiredService<ChildServiceProviderFactory>();

                factory.TryAddServiceCollection(_childContainerId, _childServices, _configureChild, _childImports);
                var thisServiceProvider = factory.GetSingletonServiceProvider(_childContainerId, services);

                var service = thisServiceProvider.GetRequiredService<TService>();

                return service;
            }
            finally
            {
                RemoveFromStack<TService>();
            }
        });

        return this;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ForwardTransient<TService>()
        where TService : class
    {
        _parentServices.AddTransient(services =>
        {
            AssertNoCycle<TService>();
            AddToStack<TService>();

            try
            {
                var factory = services.GetRequiredService<ChildServiceProviderFactory>();

                factory.TryAddServiceCollection(_childContainerId, _childServices, _configureChild, _childImports);
                var thisServiceProvider = factory.GetSingletonServiceProvider(_childContainerId, services);

                var service = thisServiceProvider.GetRequiredService<TService>();

                return service;
            }
            finally
            {
                RemoveFromStack<TService>();
            }
        });

        return this;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ImportScoped<TService>()
        where TService : class
    {
        _childImports.Add((typeof(TService), ServiceLifetime.Scoped));

        return this;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ImportSingleton<TService>()
        where TService : class
    {
        _childImports.Add((typeof(TService), ServiceLifetime.Singleton));

        return this;
    }

    /// <inheritdoc/>
    public IChildContainerBuilder ImportTransient<TService>()
        where TService : class
    {
        _childImports.Add((typeof(TService), ServiceLifetime.Transient));

        return this;
    }

    /// <summary>
    /// Adds the type to the circular dependency stack.
    /// </summary>
    /// <typeparam name="TService">The type of service.</typeparam>
    private void AddToStack<TService>()
    {
        _resolutionStack.Value!.Add(ValueTuple.Create(this, typeof(TService)));
    }

    /// <summary>
    /// Removes the type from the circular dependency stack.
    /// </summary>
    /// <typeparam name="TService">The type of service.</typeparam>
    private void RemoveFromStack<TService>()
    {
        _resolutionStack.Value!.Remove(ValueTuple.Create(this, typeof(TService)));
    }

    /// <summary>
    /// Asserts that the current service is not part of a circular dependency.
    /// </summary>
    /// <typeparam name="TService">The type of service.</typeparam>
    /// <exception cref="CircularDependencyException"></exception>
    private void AssertNoCycle<TService>()
    {
        var stack = _resolutionStack.Value!;
        if (stack.Contains(ValueTuple.Create(this, typeof(TService))))
        {
            //The service type is already in the hashset, so append it at the end as this is what closes the dependency loop
            var serviceType = typeof(TService);
            throw new CircularDependencyException(stack.Select(e => e.Item2).Append(serviceType));
        }
    }
}
