using EtherGizmos.Common.Converters;
using EtherGizmos.Common.Extensions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

public class InboxContext : DbContext
{
    public virtual DbSet<InboxMessage> InboxMessages { get; set; }

    public InboxContext(
        DbContextOptions<InboxContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //**********************************************************
        // Add Entities

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InboxContext).Assembly);

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
    }
}
