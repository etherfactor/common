namespace EtherGizmos.Common.Models;

public enum InboxStatusType
{
    Unknown = 0,
    Pending = 1,
    InFlight = 10,
    Processed = 100,
    Failed = -100,
}
