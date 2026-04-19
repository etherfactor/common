using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class DatabaseConnectionTypeExtensions
{
    extension(ConnectionType)
    {
        public static string Database => "Database";
    }
}
