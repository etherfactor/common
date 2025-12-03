using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace EtherGizmos.Common.Configuration;

public static class ModularConfigurationTypeRegistry
{
    private static readonly ConcurrentDictionary<ModularConfigurationTypeKey, ModularConfigurationTypeRegistration> _registrations = [];

    public static IReadOnlySet<ModularConfigurationTypeRegistration> Registrations => _registrations.Values.ToHashSet();

    public static void Register<TRoot, TBase>(
        string sectionName,
        string itemIdName,
        string typeName)
        where TRoot : ModularConfigurationOptions, new()
        where TBase : class
    {
        var properties = typeof(TRoot).GetProperties()
            .Where(p => typeof(TBase).IsAssignableFrom(p.PropertyType))
            .ToArray();

        var key = new ModularConfigurationTypeKey(typeof(TRoot));
        var value = new ModularConfigurationTypeRegistration(
            RootType: typeof(TRoot),
            BaseType: typeof(TBase),
            SectionName: sectionName,
            ItemIdName: itemIdName,
            TypeName: typeName,
            Properties: [.. properties]);

        _registrations.AddOrUpdate(key, (key) => value, (key, current) => value);
    }

    private record ModularConfigurationTypeKey(
        Type RootType
    );
}

public record ModularConfigurationTypeRegistration(
    Type RootType,
    Type BaseType,
    string SectionName,
    string ItemIdName,
    string TypeName,
    ImmutableList<PropertyInfo> Properties
);
