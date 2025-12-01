namespace EtherGizmos.Common.Configuration;

internal class RootPostgreSqlOptions : ConnectionOptions
{
    public PostgreSqlOptions? PostgreSql { get; set; }
}
