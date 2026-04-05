using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace EtherGizmos.Common;

[SetUpFixture]
internal static class Setup
{
    public static string PgSqlConnectionString { get; private set; }
    public static string MsSqlConnectionString { get; private set; }
    public static string MySqlConnectionString { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        try
        {
            var pgSql = new PostgreSqlBuilder("postgres:18")
                .Build();

            await pgSql.StartAsync();

            PgSqlConnectionString = pgSql.GetConnectionString();

            var msSql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await msSql.StartAsync();

            MsSqlConnectionString = msSql.GetConnectionString();

            var mySql = new MySqlBuilder("mysql:9")
                .WithCommand("--log-bin-trust-function-creators=1")
                .Build();

            await mySql.StartAsync();

            MySqlConnectionString = mySql.GetConnectionString();
        }
        catch (Exception ex)
        {
            Assert.Ignore(ex.Message);
        }
    }
}
