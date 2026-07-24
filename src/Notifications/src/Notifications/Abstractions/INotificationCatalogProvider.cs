using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationCatalogProvider
{
    NotificationCatalog GetCatalog();
}
