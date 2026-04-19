namespace EtherGizmos.Common.Models;

public enum OutboxStatusType
{
    Unknown = 0,
    Pending = 1,
    InFlight = 10,
    Published = 100,
    Failed = -100,
}
