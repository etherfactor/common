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
            var pgSql = new PostgreSqlBuilder()
                .WithImage("postgres:18")
                .Build();

            await pgSql.StartAsync();

            PgSqlConnectionString = pgSql.GetConnectionString();
        }
        catch (Exception ex)
        {
            Assert.Ignore(ex.Message);
        }
    }
}
