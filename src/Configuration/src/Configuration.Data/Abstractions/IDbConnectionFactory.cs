using EtherGizmos.Common.Configuration;
using System.Data.Common;

namespace EtherGizmos.Common.Abstractions;

public interface IDbConnectionFactory<TOptions>
    where TOptions : DatabaseConnectionOptions, new()
{
    DbConnection Create(
        TOptions options);
}
