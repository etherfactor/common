using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class SmtpEmailFromOptions
{
    [Required]
    public string Address { get; set; } = null!;

    public string? Name { get; set; }
}
