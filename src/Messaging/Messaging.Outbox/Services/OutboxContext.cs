using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Converters;
using EtherGizmos.Common.Extensions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class OutboxContext : DbContext
{
    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public OutboxContext(
        DbContextOptions<OutboxContext> options,
        [FromKeyedServices("Outbox")] IMigrationManager migrationManager) : base(options)
    {
        migrationManager.EnsureMigratedAsync().GetAwaiter().GetResult();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //**********************************************************
        // Add Entities

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OutboxContext).Assembly);

        //**********************************************************
        // Add Value Converters

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new ObjectToInferredTypesConverter());

        modelBuilder.AddGlobalValueConverter(new ValueConverter<DateTimeOffset, DateTime>(
            app => app.UtcDateTime,
            db => new DateTimeOffset(db, TimeSpan.Zero)));

        modelBuilder.AddGlobalValueConverter(new ValueConverter<DateTimeOffset?, DateTime?>(
            app => app != null ? app.Value.UtcDateTime : null,
            db => db != null ? new DateTimeOffset((DateTime)db, TimeSpan.Zero) : null));

        modelBuilder.AddGlobalValueConverter(new ValueConverter<IDictionary<string, object?>, string>(
            app => JsonSerializer.Serialize(app, jsonOptions),
            db => JsonSerializer.Deserialize<IDictionary<string, object?>>(db, jsonOptions)!),
            new ValueComparer<IDictionary<string, object?>>(
                (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => new Dictionary<string, object?>(c)));

        modelBuilder.AddGlobalValueConverter(new ValueConverter<IDictionary<string, string?>, string>(
            app => JsonSerializer.Serialize(app, jsonOptions),
            db => JsonSerializer.Deserialize<IDictionary<string, string?>>(db, jsonOptions)!),
            new ValueComparer<IDictionary<string, string?>>(
                (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => new Dictionary<string, string?>(c)));
    }
}
