namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS通信配置实体（单例模式）
/// DWS communication configuration entity (Singleton pattern)
/// </summary>
public record class DwsConfig
{
    /// <summary>
    /// 单例配置ID（固定为"DwsConfigId"）
    /// Singleton configuration ID (Fixed as "DwsConfigId")
    /// </summary>
    public const string SingletonId = "DwsConfigId";
    
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    public string ConfigId { get; init; } = SingletonId;
    /// 通信模式：Server（服务端）或 Client（客户端）
    /// Communication mode: Server or Client
    public required string Mode { get; init; }
    /// 主机地址（服务端监听地址或客户端连接地址）
    /// Host address (Server listen address or Client connect address)
    public required string Host { get; init; }
    /// 端口号
    /// Port number
    public required int Port { get; init; }
    /// 数据模板ID，关联到 DwsDataTemplate
    /// Data template ID, links to DwsDataTemplate
    public required long DataTemplateId { get; init; }
    /// 是否启用
    /// Is enabled
    public required bool IsEnabled { get; init; }
    /// 最大连接数（仅服务端模式）
    /// Maximum connections (Server mode only)
    public int MaxConnections { get; init; } = 1000;
    /// 接收缓冲区大小（字节）
    /// Receive buffer size (bytes)
    public int ReceiveBufferSize { get; init; } = 8192;
    /// 发送缓冲区大小（字节）
    /// Send buffer size (bytes)
    public int SendBufferSize { get; init; } = 8192;
    /// 连接超时时间（秒）
    /// Connection timeout (seconds)
    public int TimeoutSeconds { get; init; } = 30;
    /// 是否自动重连（仅客户端模式）
    /// Auto reconnect (Client mode only)
    public bool AutoReconnect { get; init; } = true;
    /// 重连间隔（秒）
    /// Reconnect interval (seconds)
    public int ReconnectIntervalSeconds { get; init; } = 5;
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
