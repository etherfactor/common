using Npgsql;
using Testcontainers.PostgreSql;

namespace EtherGizmos.Common;

[SetUpFixture]
internal static class Setup
{
    public static string PgSqlConnectionString { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        try
        {
            var pgSql = new PostgreSqlBuilder("postgres:18")
                .Build();

            await pgSql.StartAsync();

            PgSqlConnectionString = pgSql.GetConnectionString();
        }
        catch (Exception ex)
        {
            Assert.Ignore(ex.Message);
        }
    }

    public static async Task<string> CreateDatabase(
        string database)
    {
        using var connection = new NpgsqlConnection(PgSqlConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"create database {database}";
        await command.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(PgSqlConnectionString)
        {
            Database = database,
        };

        return builder.ToString();
    }
}
