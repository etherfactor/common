using Testcontainers.RabbitMq;

namespace EtherGizmos.Common;

[SetUpFixture]
internal static class Setup
{
    public static string RmqConnectionString { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        try
        {
            var rmq = new RabbitMqBuilder("rabbitmq:4")
                .Build();

            await rmq.StartAsync();

            RmqConnectionString = rmq.GetConnectionString();
        }
        catch (Exception ex)
        {
            Assert.Ignore(ex.Message);
        }
    }
}
