namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// DWS配置响应DTO（单例模式，不暴露ID）
/// DWS configuration response DTO (Singleton pattern, ID not exposed)
/// </summary>
public record DwsConfigResponseDto
{
    // ID字段已移除，采用单例模式
    // ID field removed, using singleton pattern
    
    public required string Mode { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required long DataTemplateId { get; init; }
    public required bool IsEnabled { get; init; }
    public int MaxConnections { get; init; }
    public int ReceiveBufferSize { get; init; }
    public int SendBufferSize { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool AutoReconnect { get; init; }
    public int ReconnectIntervalSeconds { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
