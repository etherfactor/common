using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Configuration;

public class DigestScheduleConfig
{
    [Required]
    public string CronExpression { get; set; } = null!;
}
