namespace EtherGizmos.Common.Abstractions;

public enum MessageDecision
{
    None = 0,
    Complete = 1,
    Abandon = 2,
    DeadLetter = 3,
}
