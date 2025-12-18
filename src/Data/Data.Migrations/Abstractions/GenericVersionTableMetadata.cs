using FluentMigrator.Runner.VersionTableInfo;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Abstractions;

/// <summary>
/// Overrides default FluentMigrator version table naming.
/// </summary>
public class GenericVersionTableMetadata : IVersionTableMetaData
{
    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public virtual object? ApplicationContext { get; set; }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public virtual bool OwnsSchema => true;

    /// <inheritdoc/>
    public virtual string SchemaName => "dbo";

    /// <inheritdoc/>
    public virtual string TableName => "migration_history";

    /// <inheritdoc/>
    public virtual string ColumnName => "version";

    /// <inheritdoc/>
    public virtual string DescriptionColumnName => "description";

    /// <inheritdoc/>
    public virtual string UniqueIndexName => "UX_migration_history_version";

    /// <inheritdoc/>
    public virtual string AppliedOnColumnName => "applied_at_utc";

    /// <inheritdoc/>
    public virtual bool CreateWithPrimaryKey => false;
}

/// <summary>
/// Overrides default FluentMigrator version table naming. Postgres flavor.
/// </summary>
public class PostgresVersionTableMetadata : GenericVersionTableMetadata
{
    /// <inheritdoc/>
    public override string SchemaName => "public";
}
