using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class PfxRawCertificateOptions : AsymmetricKeyOptions
{
    [Required]
    public string CertificateBase64 { get; set; } = null!;

    public string? Password { get; set; }
}
