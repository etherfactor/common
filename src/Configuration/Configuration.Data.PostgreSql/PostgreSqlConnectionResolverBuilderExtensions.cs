using EtherGizmos.Common;
using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class PostgreSqlConnectionResolverBuilderExtensions
{
    extension(IConnectionResolverBuilder @this)
    {
        public IConnectionResolverBuilder WithPostgreSql()
        {
            @this.Services.TryAddSingleton<IConnectionDbConnectionFactory<PostgreSqlOptions>, PostgreSqlDbConnectionFactory>();
            @this.Services
                .Configure<ConnectionTypeOptions>(opt =>
                {
                    opt.RootConnectionTypes.Add(typeof(RootPostgreSqlOptions));
                });

            return @this;
        }
    }
}
