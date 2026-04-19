using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

public sealed class FileAttachmentSource : IEmailAttachmentSource
{
    private readonly string _filePath;

    public FileAttachmentSource(
        string filePath)
    {
        _filePath = filePath;
    }

    public Task<Stream> OpenReadAsync(CancellationToken ct)
        => Task.FromResult<Stream>(File.OpenRead(_filePath));

    public long Length => new FileInfo(_filePath).Length;
}
