namespace EtherGizmos.Common.Configuration;

public class RootSmtpEmailOptions : ConnectionOptions
{
    public SmtpEmailOptions? Smtp { get; set; }
}
