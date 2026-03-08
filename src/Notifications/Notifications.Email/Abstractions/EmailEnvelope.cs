namespace EtherGizmos.Common.Abstractions;

public class EmailEnvelope : INotificationEnvelope<EmailMethod>
{
    public EmailMessage Message { get; set; }

    public EmailEnvelope(
        EmailMessage message)
    {
        Message = message;
    }
}
