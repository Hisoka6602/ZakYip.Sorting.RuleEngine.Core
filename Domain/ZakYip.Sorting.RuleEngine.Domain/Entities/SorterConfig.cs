namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣机通信配置实体（单例模式）
/// Sorter communication configuration entity (Singleton pattern)
/// </summary>
public record class SorterConfig
{
    /// <summary>
    /// 单例配置ID（固定为"SorterConfigId"）
    /// Singleton configuration ID (Fixed as "SorterConfigId")
    /// </summary>
    public const string SingletonId = "SorterConfigId";
    
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    public string ConfigId { get; init; } = SingletonId;
    /// 通信协议类型：TCP / HTTP / SignalR
    /// Communication protocol type: TCP / HTTP / SignalR
    public required string Protocol { get; init; }
    /// 连接模式：Server / Client
    /// Connection mode: Server / Client
    /// Server 模式：RuleEngine 监听端口，等待下游连接
    /// Client 模式：RuleEngine 主动连接到下游
    public string ConnectionMode { get; init; } = "Client";
    /// 主机地址
    /// Host address
    /// Server 模式：监听地址（如 0.0.0.0）
    /// Client 模式：下游服务器地址（如 192.168.1.100）
    public required string Host { get; init; }
    /// 端口号
    /// Port number
    public required int Port { get; init; }
    /// 是否启用
    /// Is enabled
    public required bool IsEnabled { get; init; }
    /// 连接超时时间（秒）
    /// Connection timeout (seconds)
    public int TimeoutSeconds { get; init; } = 30;
    /// 是否自动重连
    /// Auto reconnect
    public bool AutoReconnect { get; init; } = true;
    /// 重连间隔（秒）
    /// Reconnect interval (seconds)
    public int ReconnectIntervalSeconds { get; init; } = 5;
    /// 心跳间隔（秒）
    /// Heartbeat interval (seconds)
    public int HeartbeatIntervalSeconds { get; init; } = 10;
    /// 备注说明
    /// Description
    public string? Description { get; init; }
    /// 创建时间
    /// Created time
    public required DateTime CreatedAt { get; init; }
    /// 最后更新时间
    /// Last updated time
    public required DateTime UpdatedAt { get; init; }
}
