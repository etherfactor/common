using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

/// <summary>
/// Provides methods to pass services back and forth between a parent and a child service container. Does not initialize any scopes.
/// </summary>
public interface IChildContainerBuilder
{
    /// <summary>
    /// The child service container being built. This will eventually be materialized into the child service provider.
    /// </summary>
    IServiceCollection ChildServices { get; }

    /// <summary>
    /// Forwards a scoped service from the child container back to the parent.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardScoped<TService>()
        where TService : class;

    /// <summary>
    /// Forwards a scoped service from the child container back to the parent, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedScoped<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Forwards a scoped service from the child container back to the parent, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedScoped<TService>(
        object? childServiceKey,
        object? parentServiceKey)
        where TService : class;

    /// <summary>
    /// Forwards a singleton service from the child container back to the parent.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardSingleton<TService>()
        where TService : class;

    /// <summary>
    /// Forwards a singleton service from the child container back to the parent, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedSingleton<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Forwards a singleton service from the child container back to the parent, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedSingleton<TService>(
        object? childServiceKey,
        object? parentServiceKey)
        where TService : class;

    /// <summary>
    /// Forwards a transient service from the child container back to the parent.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardTransient<TService>()
        where TService : class;

    /// <summary>
    /// Forwards a transient service from the child container back to the parent, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedTransient<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Forwards a transient service from the child container back to the parent, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being forwarded.</typeparam>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ForwardKeyedTransient<TService>(
        object? childServiceKey,
        object? parentServiceKey)
        where TService : class;

    /// <summary>
    /// Imports a scoped service from the parent container into the child.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportScoped<TService>()
        where TService : class;

    /// <summary>
    /// Imports a scoped service from the parent container into the child, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedScoped<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Imports a scoped service from the parent container into the child, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedScoped<TService>(
        object? parentServiceKey,
        object? childServiceKey)
        where TService : class;

    /// <summary>
    /// Imports a singleton service from the parent container into the child.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportSingleton<TService>()
        where TService : class;

    /// <summary>
    /// Imports a singleton service from the parent container into the child, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedSingleton<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Imports a singleton service from the parent container into the child, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedSingleton<TService>(
        object? parentServiceKey,
        object? childServiceKey)
        where TService : class;

    /// <summary>
    /// Imports a transient service from the parent container into the child.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportTransient<TService>()
        where TService : class;

    /// <summary>
    /// Imports a transient service from the parent container into the child, using a service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="serviceKey">The service key on both ends. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedTransient<TService>(
        object? serviceKey)
        where TService : class;

    /// <summary>
    /// Imports a transient service from the parent container into the child, optionally remapping the service key.
    /// </summary>
    /// <typeparam name="TService">The type of service being imported.</typeparam>
    /// <param name="parentServiceKey">The service key in the parent provider. Use <see langword="null"/> for an unkeyed service.</param>
    /// <param name="childServiceKey">The service key in the child container. Use <see langword="null"/> for an unkeyed service.</param>
    /// <returns>The builder.</returns>
    IChildContainerBuilder ImportKeyedTransient<TService>(
        object? parentServiceKey,
        object? childServiceKey)
        where TService : class;
}
