namespace EtherGizmos.Common.Abstractions;

public interface IEmailAttachmentSource
{
    Task<Stream> OpenReadAsync(
        CancellationToken cancellationToken = default);

    long Length { get; }
}
