using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

public static class OutboxMessagingBuilderExtensions
{
    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder UseOutbox()
        {
            //Replace the default sender, so we can intercept the message before it actually sends
            @this.Services.Decorate<IMessageSender, OutboxMessageSender>();

            return @this;
        }
    }
}
