using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class MessagingBuilder : IMessagingBuilder
{
    public string BusId { get; }

    public IServiceCollection Services { get; }

    public MessagingBuilder(
        string busId,
        IServiceCollection services)
    {
        BusId = busId;
        Services = services;
    }
}
