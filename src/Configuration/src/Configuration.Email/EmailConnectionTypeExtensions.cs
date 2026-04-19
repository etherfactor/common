using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class EmailConnectionTypeExtensions
{
    extension(ConnectionType)
    {
        public static string Email => "Email";
    }
}
