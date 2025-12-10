namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 分拣机配置响应DTO（单例模式，不暴露ID）
/// Sorter configuration response DTO (Singleton pattern, ID not exposed)
/// </summary>
public record SorterConfigResponseDto
{
    // ID字段已移除，采用单例模式
    // ID field removed, using singleton pattern
    
    public required string Name { get; init; }
    public required string Protocol { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required bool IsEnabled { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool AutoReconnect { get; init; }
    public int ReconnectIntervalSeconds { get; init; }
    public int HeartbeatIntervalSeconds { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
