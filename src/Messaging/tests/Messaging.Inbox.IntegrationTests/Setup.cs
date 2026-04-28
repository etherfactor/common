using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace EtherGizmos.Common;

[SetUpFixture]
internal static class Setup
{
    public static string RmqConnectionString { get; private set; }

    public static string PgSqlConnectionString { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        try
        {
            var rmq = new RabbitMqBuilder("rabbitmq:4")
                .Build();

            await rmq.StartAsync();

            RmqConnectionString = rmq.GetConnectionString();

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
}
