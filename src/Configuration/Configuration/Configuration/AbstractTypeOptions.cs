using System.Collections.Concurrent;

namespace EtherGizmos.Common.Configuration;

public static class AbstractTypeOptions
{
    private static readonly ConcurrentDictionary<Type, HashSet<Type>> _connectionsMap = [];

    public static HashSet<Type> GetRootTypesFor<TOptions>()
        where TOptions : AbstractOptions, new()
        => GetRootTypesFor(typeof(TOptions));

    public static HashSet<Type> GetRootTypesFor(
        Type abstractOptionsType)
        => _connectionsMap.GetOrAdd(abstractOptionsType, []);

    public static void RegisterType<TOptions, TSubOptions>()
        where TOptions : AbstractOptions, new()
        where TSubOptions : class, new()
        => RegisterType(typeof(TOptions), typeof(TSubOptions));

    public static void RegisterType(
        Type abstractOptionsType,
        Type rootOptionsType)
        => GetRootTypesFor(abstractOptionsType).Add(rootOptionsType);
}
