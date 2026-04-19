using EtherGizmos.Common.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common;

public static class SmtpEmailConnectionOptionsExtensions
{
    extension(EmailConnectionOptions @this)
    {
        public bool IsSmtp(
            [NotNullWhen(true)] out SmtpEmailOptions? options)
        {
            if (@this is SmtpEmailOptions typed)
            {
                options = typed;
                return true;
            }

            options = null;
            return false;
        }
    }
}
