using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public abstract class AbstractOptions
{
    [Required]
    public virtual string Type { get; set; } = null!;
}
