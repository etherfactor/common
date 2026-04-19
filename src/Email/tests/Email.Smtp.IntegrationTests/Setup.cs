using DotNet.Testcontainers.Builders;

namespace EtherGizmos.Common;

[SetUpFixture]
internal static class Setup
{
    public static HttpClient MailHogClient { get; private set; }
    public static string MailHogHost { get; private set; }
    public static int MailHogPort { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        try
        {
            var mailhog = new ContainerBuilder("mailhog/mailhog:latest")
                .WithPortBinding(1025, true)
                .WithPortBinding(8025, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1025))
                .Build();

            await mailhog.StartAsync();

            MailHogHost = mailhog.Hostname;
            MailHogPort = mailhog.GetMappedPublicPort(1025);

            var httpPort = mailhog.GetMappedPublicPort(8025);
            var baseUrl = $"http://{mailhog.Hostname}:{httpPort}";

            MailHogClient = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl),
            };
        }
        catch (Exception ex)
        {
            Assert.Ignore(ex.Message);
        }
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        MailHogClient?.Dispose();
    }
}
