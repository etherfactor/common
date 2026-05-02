using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class InMemoryAttachmentSource : IEmailAttachmentSource
{
    private readonly byte[] _data;

    public InMemoryAttachmentSource(
        byte[] data)
    {
        _data = data;
    }

    public Task<Stream> OpenReadAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(new MemoryStream(_data, writable: false));

    public long Length => _data.Length;
}
