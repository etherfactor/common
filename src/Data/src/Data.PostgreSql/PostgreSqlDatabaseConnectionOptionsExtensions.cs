using EtherGizmos.Common.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common;

public static class PostgreSqlDatabaseConnectionOptionsExtensions
{
    extension(DatabaseConnectionOptions @this)
    {
        public bool IsPostgreSql(
            [NotNullWhen(true)] out PostgreSqlOptions? options)
        {
            if (@this is PostgreSqlOptions typed)
            {
                options = typed;
                return true;
            }

            options = null;
            return false;
        }
    }
}
