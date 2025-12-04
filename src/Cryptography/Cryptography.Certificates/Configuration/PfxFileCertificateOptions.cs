using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class PfxFileCertificateOptions : AsymmetricKeyOptions
{
    [Required]
    public string Path { get; set; } = null!;

    public string? Password { get; set; }

    public bool AutoGenerate { get; set; } = false;
}
