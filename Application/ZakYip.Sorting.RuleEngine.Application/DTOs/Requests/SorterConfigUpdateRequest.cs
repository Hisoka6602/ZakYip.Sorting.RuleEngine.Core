namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 分拣机配置更新请求DTO
/// Sorter configuration update request DTO
/// </summary>
public record SorterConfigUpdateRequest
{
    public required string Protocol { get; init; }
    
    /// <summary>
    /// 连接模式：Server 或 Client
    /// Connection mode: Server or Client
    /// </summary>
    public string ConnectionMode { get; init; } = "Client";
    
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required bool IsEnabled { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public bool AutoReconnect { get; init; } = true;
    public int ReconnectIntervalSeconds { get; init; } = 5;
    public int HeartbeatIntervalSeconds { get; init; } = 10;
    public string? Description { get; init; }
}
