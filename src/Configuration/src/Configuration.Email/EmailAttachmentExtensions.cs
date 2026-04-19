using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;

namespace EtherGizmos.Common;

public static class EmailAttachmentExtensions
{
    extension(EmailAttachment)
    {
        public static EmailAttachment FromBytes(
            string fileName,
            byte[] bytes)
        {
            var source = new InMemoryAttachmentSource(bytes);
            return new EmailAttachment(fileName, source);
        }

        public static EmailAttachment FromFile(
            string filePath)
        {
            var source = new FileAttachmentSource(filePath);
            var fileName = Path.GetFileName(filePath);
            return new EmailAttachment(fileName, source);
        }
    }
}
