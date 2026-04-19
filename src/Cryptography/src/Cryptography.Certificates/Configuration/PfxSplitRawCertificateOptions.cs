using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class PfxSplitRawCertificateOptions : AsymmetricKeyOptions
{
    [Required]
    public string PublicKeyBase64 { get; set; } = null!;

    [Required]
    public string PrivateKeyBase64 { get; set; } = null!;
}
