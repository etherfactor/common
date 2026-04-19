using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class SmtpEmailOptions : EmailConnectionOptions
{
    [Required]
    public string Host { get; set; } = null!;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }
}
