namespace EtherGizmos.Common.Abstractions;

/// <summary>
/// Overrides default FluentMigrator version table naming. Postgres flavor.
/// </summary>
public class PostgresVersionTableMetadata : GenericVersionTableMetadata
{
    /// <inheritdoc/>
    public override string SchemaName => "public";
}
