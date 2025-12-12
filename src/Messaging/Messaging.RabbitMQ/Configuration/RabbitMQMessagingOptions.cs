namespace EtherGizmos.Common.Configuration;

public class RabbitMQMessagingOptions
{
    public string? ConnectionString { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Host { get; set; }

    public int Port { get; set; } = 5672;
}
