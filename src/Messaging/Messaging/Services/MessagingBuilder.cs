using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class MessagingBuilder : IMessagingBuilder
{
    public IServiceCollection Services { get; }

    public MessagingBuilder(
        IServiceCollection services)
    {
        Services = services;
    }
}
