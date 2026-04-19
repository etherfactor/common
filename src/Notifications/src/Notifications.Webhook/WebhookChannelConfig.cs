using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common;

public class WebhookChannelConfig
{
    [Required]
    public string Method { get; set; } = "POST";

    [Required]
    public string Endpoint { get; set; } = null!;

    [Required]
    public Dictionary<string, string> Headers { get; set; } = [];
}
