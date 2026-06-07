using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal record ChildServiceRegistration(
    Type ServiceType, ServiceLifetime Lifetime, object? ParentServiceKey = null, object? ChildServiceKey = null);
