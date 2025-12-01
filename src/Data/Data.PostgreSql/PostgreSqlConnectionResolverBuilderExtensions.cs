using EtherGizmos.Common;
using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class PostgreSqlConnectionResolverBuilderExtensions
{
    extension(IConnectionResolverBuilder @this)
    {
        public IConnectionResolverBuilder WithPostgreSql()
        {
            @this.Services.TryAddSingleton<IDbConnectionFactory<PostgreSqlOptions>, PostgreSqlDbConnectionFactory>();
            AbstractTypeOptions.RegisterType<ConnectionOptions, RootPostgreSqlOptions>();
            //@this.Services
            //    .Configure<AbstractTypeOptions>(opt =>
            //    {
            //        var set = opt.ConnectionMap.GetOrAdd(typeof(ConnectionOptions), []);
            //        set.Add(typeof(RootPostgreSqlOptions));
            //    });

            return @this;
        }
    }
}
