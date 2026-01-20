using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationTypeBuilder
{
    string EventType { get; }
    
    IServiceCollection Services { get; }
}
