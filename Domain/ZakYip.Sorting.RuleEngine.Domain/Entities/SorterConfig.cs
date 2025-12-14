namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣机通信配置实体（单例模式）
/// Sorter communication configuration entity (Singleton pattern)
/// </summary>
public record class SorterConfig
{
    /// <summary>
    /// 单例配置ID（固定为1）
    /// Singleton configuration ID (Fixed as 1)
    /// </summary>
    public const long SingletonId = 1L;
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public long ConfigId { get; init; } = SingletonId;

    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 通信协议类型：TCP / HTTP / SignalR
    /// Communication protocol type: TCP / HTTP / SignalR
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// 连接模式：Server / Client
    /// Connection mode: Server / Client
    /// Server 模式：RuleEngine 监听端口，等待下游连接
    /// Client 模式：RuleEngine 主动连接到下游
    /// </summary>
    public string ConnectionMode { get; init; } = "Client";

    /// <summary>
    /// 主机地址
    /// Host address
    /// Server 模式：监听地址（如 0.0.0.0）
    /// Client 模式：下游服务器地址（如 192.168.1.100）
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// 端口号
    /// Port number
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// 是否启用
    /// Is enabled
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 连接超时时间（秒）
    /// Connection timeout (seconds)
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// 是否自动重连
    /// Auto reconnect
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    /// 重连间隔（秒）
    /// Reconnect interval (seconds)
    /// </summary>
    public int ReconnectIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// 心跳间隔（秒）
    /// Heartbeat interval (seconds)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; init; } = 10;

    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 创建时间
    /// Created time
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// Last updated time
    /// </summary>
    public required DateTime UpdatedAt { get; init; }
}
