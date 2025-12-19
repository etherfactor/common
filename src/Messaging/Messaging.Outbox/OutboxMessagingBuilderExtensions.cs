using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

public static class OutboxMessagingBuilderExtensions
{
    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder UseOutbox(string databaseConnectionId)
        {
            //Replace the default sender, so we can intercept the message before it actually sends
            @this.Services.Decorate<IMessageSender, OutboxMessageSender>();

            @this.Services
                .AddDbContext<OutboxContext>((provider, opt) =>
                {
                    opt.UseConnection(provider, "Outbox", opt =>
                    {
                        opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });

            @this.Services
                .AddUnitOfWork(opt =>
                {
                    opt.BindDbContext<OutboxContext>();
                });

            return @this;
        }
    }
}
