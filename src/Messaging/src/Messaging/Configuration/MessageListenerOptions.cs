using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class MessageListenerOptions
{
    public bool IsTopic { get; set; } = false;

    [Required]
    public string Name { get; set; } = null!;

    public string? Subscription { get; set; }
}
