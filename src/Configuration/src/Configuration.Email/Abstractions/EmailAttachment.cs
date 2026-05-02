namespace EtherGizmos.Common.Abstractions;

public sealed record EmailAttachment(string FileName, IEmailAttachmentSource source)
{
    public string ContentType { get; init; } = "application/octet-stream";
}
