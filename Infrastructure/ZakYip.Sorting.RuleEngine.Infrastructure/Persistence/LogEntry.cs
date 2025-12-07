namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 日志实体
/// Log entry entity
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
