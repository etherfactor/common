using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class KeyTypeExtensions
{
    extension(KeyType)
    {
        public static string Asymmetric => "Asymmetric";

        public static string Symmetric => "Symmetric";
    }
}
