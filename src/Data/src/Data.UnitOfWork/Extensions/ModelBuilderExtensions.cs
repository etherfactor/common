using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EtherGizmos.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds a value converter to all properties of a given type.
    /// </summary>
    /// <typeparam name="TProvider">The database type.</typeparam>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="this">Itself.</param>
    /// <param name="converter">The converter to use.</param>
    /// <param name="comparer">Determines if two values are equivalent.</param>
    /// <returns>Itself.</returns>
    public static ModelBuilder AddGlobalValueConverter<TProvider, TModel>(
        this ModelBuilder @this,
        ValueConverter<TModel, TProvider> converter,
        ValueComparer<TModel>? comparer = null)
    {
        foreach (var entityType in @this.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(TModel))
                {
                    property.SetValueConverter(converter);

                    if (comparer is not null)
                    {
                        property.SetValueComparer(comparer);
                    }
                }
            }
        }

        return @this;
    }
}
