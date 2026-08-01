namespace EtherGizmos.Common.Services;

internal enum MessageBusLifecycleState
{
    Created = 0,
    Running = 1,
    Draining = 2,
    Stopping = 3,
    Stopped = 4,
}
