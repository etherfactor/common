using EtherGizmos.Common.Configuration;
using System.Data.Common;

namespace EtherGizmos.Common.Abstractions;

public interface IConnectionDbConnectionFactory<TOptions>
    where TOptions : DatabaseConnectionOptions, new()
{
    DbConnection Create(
        TOptions options);
}
