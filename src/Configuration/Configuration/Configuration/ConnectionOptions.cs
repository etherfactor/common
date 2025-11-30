using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class ConnectionOptions
{
    [Required]
    public virtual string Type { get; set; } = null!;
}
