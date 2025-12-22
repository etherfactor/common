using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class InboxMessagingBuilderExtensions
{
    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder UseInbox(string databaseConnectionId)
        {
            @this.Services
                .AddDbContext<InboxContext>((provider, opt) =>
                {
                    opt.UseConnection(provider, databaseConnectionId, opt =>
                    {
                        opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });

            @this.Services
                .AddUnitOfWork(opt =>
                {
                    opt.BindDbContext<InboxContext>();
                });

            @this.Services
                .AddMigrations("Inbox", typeof(InboxMessagingBuilderExtensions).Assembly!)
                .UseConnection(databaseConnectionId);

            @this.AddTransformer<InboxSettlementTransformer>();
            @this.AddMiddleware<InboxSettlementMiddleware>();
            @this.AddMiddleware<InboxDeduplicateMiddleware>();

            @this.Services.TryAddScoped<IMessageSettlement, MessageSettlement>();

            return @this;
        }
    }
}
