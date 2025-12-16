using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace EtherGizmos.Common;

public static class MessagingBuilderExtensions
{
    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder AddMiddleware<TMiddleware>(
            string? logicalName = null)
            where TMiddleware : class, IMessageMiddleware
        {
            var key = new BusKey(@this.BusId, logicalName);
            @this.Services.AddKeyedScoped<IMessageMiddleware, TMiddleware>(key);

            return @this;
        }

        public IMessagingBuilder AddTransformer<TTransformer>(
            string? logicalName = null)
            where TTransformer : class, IMessageTransformer
        {
            var key = new BusKey(@this.BusId, logicalName);
            @this.Services.AddKeyedScoped<IMessageTransformer, TTransformer>(key);

            return @this;
        }

        public IMessagingBuilder UseSerializer<TSerializer>(
            string? logicalName = null)
            where TSerializer : class, IMessageSerializer
        {
            var key = new BusKey(@this.BusId, logicalName);
            @this.Services.AddKeyedSingleton<IMessageSerializer, TSerializer>(key);

            return @this;
        }

        public IMessagingBuilder AddConsumersFromAssemblies(
            params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var consumers = assembly.GetTypes()
                    .Where(type =>
                        type.GetInterfaces().Any(intf =>
                            intf.IsGenericType &&
                            intf.GetGenericTypeDefinition() == typeof(IMessageConsumer<>)));

                foreach (var consumer in consumers)
                {
                    var types = consumer.GetInterfaces()
                        .Where(type =>
                            type.IsGenericType &&
                            type.GetGenericTypeDefinition() == typeof(IMessageConsumer<>));

                    foreach (var type in types)
                    {
                        @this.Services.TryAddScoped(type, consumer);
                    }
                }
            }

            return @this;
        }

        public IMessagingBuilder UseConnection(
            string connectionId)
        {
            @this.Services.AddKeyedSingleton(@this.BusId, (provider, _) =>
            {
                var connectionResolver = provider.GetRequiredService<IConnectionResolver>();
                var connection = connectionResolver.GetMessagingConnection(connectionId);
                var type = connection.GetType();

                var result = (IMessagePublisherFactory)typeof(MessagingBuilderExtensions)
                    .GetMethod(nameof(InnerUseConnectionPublisher), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod([type])
                    .Invoke(null, [@this, provider, @this.BusId, connection])!;

                return result;
            });

            @this.Services.AddKeyedSingleton(@this.BusId, (provider, _) =>
            {
                var connectionResolver = provider.GetRequiredService<IConnectionResolver>();
                var connection = connectionResolver.GetMessagingConnection(connectionId);
                var type = connection.GetType();

                var result = (IMessageListenerFactory)typeof(MessagingBuilderExtensions)
                    .GetMethod(nameof(InnerUseConnectionListener), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod([type])
                    .Invoke(null, [@this, provider, @this.BusId, connection])!;

                return result;
            });

            return @this;
        }

        internal IMessagePublisherFactory InnerUseConnectionPublisher<TOptions>(
            IServiceProvider provider,
            string busId,
            TOptions options)
            where TOptions : MessagingConnectionOptions, new()
        {
            var factory = provider.GetService<IMessagePublisherFactoryBuilder<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for creating a message publisher for type {typeof(TOptions)}");

            return factory.CreatePublisherFactory(busId, options);
        }

        internal IMessageListenerFactory InnerUseConnectionListener<TOptions>(
            IServiceProvider provider,
            string busId,
            TOptions options)
            where TOptions : MessagingConnectionOptions, new()
        {
            var factory = provider.GetService<IMessageListenerFactoryBuilder<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for creating a message listener for type {typeof(TOptions)}");

            return factory.CreateListenerFactory(busId, options);
        }
    }
}
