using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class DeliveryModeExtensions
{
    extension(DeliveryModes)
    {
        public static DigestMode Digest => DigestMode.Instance;

        public static ImmediateMode Immediate => ImmediateMode.Instance;
    }
}
