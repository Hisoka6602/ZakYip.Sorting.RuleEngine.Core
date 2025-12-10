namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// DWS配置更新请求DTO
/// DWS configuration update request DTO
/// </summary>
public record DwsConfigUpdateRequest
{
    public required string Name { get; init; }
    public required string Mode { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required long DataTemplateId { get; init; }
    public required bool IsEnabled { get; init; }
    public int MaxConnections { get; init; } = 1000;
    public int ReceiveBufferSize { get; init; } = 8192;
    public int SendBufferSize { get; init; } = 8192;
    public int TimeoutSeconds { get; init; } = 30;
    public bool AutoReconnect { get; init; } = true;
    public int ReconnectIntervalSeconds { get; init; } = 5;
    public string? Description { get; init; }
}
