using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

            @this.AddMiddleware<InboxDeduplicateMiddleware>();

            return @this;
        }
    }
}
