namespace EtherGizmos.Common.Abstractions;

public sealed record EmailMessage
{
    public EmailAddress From { get; init; } = EmailAddress.Empty;
    public IReadOnlyList<EmailAddress> To { get; init; } = [];
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];

    public string Subject { get; init; } = "";
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }

    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = Array.Empty<EmailAttachment>();
}
