using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;

namespace EtherGizmos.Common.Configuration;

public static class AbstractTypeRegistry
{
    private static readonly ConcurrentDictionary<AbstractTypeKey, AbstractTypeRegistration> _registrations = [];

    public static IReadOnlySet<AbstractTypeRegistration> Registrations => _registrations.Values.ToHashSet();

    public static void Register<TRoot, TBase>(
        string sectionName,
        string itemIdName,
        string typeName)
        where TRoot : AbstractOptions, new()
        where TBase : class
    {
        var properties = typeof(TRoot).GetProperties()
            .Where(p => typeof(TBase).IsAssignableFrom(p.PropertyType))
            .ToArray();

        var key = new AbstractTypeKey(typeof(TRoot));
        var value = new AbstractTypeRegistration(
            RootType: typeof(TRoot),
            BaseType: typeof(TBase),
            SectionName: sectionName,
            ItemIdName: itemIdName,
            TypeName: typeName,
            Properties: [.. properties]);

        _registrations.AddOrUpdate(key, (key) => value, (key, current) => value);
    }

    private record AbstractTypeKey(
        Type RootType
    );
}

public record AbstractTypeRegistration(
    Type RootType,
    Type BaseType,
    string SectionName,
    string ItemIdName,
    string TypeName,
    ImmutableList<PropertyInfo> Properties
);
