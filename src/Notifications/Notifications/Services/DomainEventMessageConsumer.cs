using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Services;

internal class DomainEventMessageConsumer : IMessageConsumer<DomainEventMessage>
{
    public Task ConsumeAsync(
        IMessageContext<DomainEventMessage> context)
    {
        throw new NotImplementedException();
    }
}
