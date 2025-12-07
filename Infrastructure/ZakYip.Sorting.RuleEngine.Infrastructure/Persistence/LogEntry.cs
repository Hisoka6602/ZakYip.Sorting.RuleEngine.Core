namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;

/// <summary>
/// 日志实体
/// Log entry entity
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public required string Level { get; init; } = string.Empty;
    public required string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}
