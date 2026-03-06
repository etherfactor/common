using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common.Models;

public class DigestScheduleConfig
{
    [Required]
    public string CronExpression { get; set; } = null!;
}
