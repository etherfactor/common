namespace EtherGizmos.Common.Abstractions;

public record EmailEnvelope(EmailMessage Message)
    : INotificationEnvelope<EmailMethod>;
