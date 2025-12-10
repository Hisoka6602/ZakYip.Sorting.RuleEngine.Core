namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS通信配置实体
/// DWS communication configuration entity
/// </summary>
public record class DwsConfig
{
    /// <summary>
    /// 配置ID（主键）
    /// Configuration ID (Primary Key)
    /// </summary>
    public required string ConfigId { get; init; }

    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 通信模式：Server（服务端）或 Client（客户端）
    /// Communication mode: Server or Client
    /// </summary>
    public required string Mode { get; init; }

    /// <summary>
    /// 主机地址（服务端监听地址或客户端连接地址）
    /// Host address (Server listen address or Client connect address)
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// 端口号
    /// Port number
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// 数据模板ID，关联到 DwsDataTemplate
    /// Data template ID, links to DwsDataTemplate
    /// </summary>
    public required string DataTemplateId { get; init; }

    /// <summary>
    /// 是否启用
    /// Is enabled
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 最大连接数（仅服务端模式）
    /// Maximum connections (Server mode only)
    /// </summary>
    public int MaxConnections { get; init; } = 1000;

    /// <summary>
    /// 接收缓冲区大小（字节）
    /// Receive buffer size (bytes)
    /// </summary>
    public int ReceiveBufferSize { get; init; } = 8192;

    /// <summary>
    /// 发送缓冲区大小（字节）
    /// Send buffer size (bytes)
    /// </summary>
    public int SendBufferSize { get; init; } = 8192;

    /// <summary>
    /// 连接超时时间（秒）
    /// Connection timeout (seconds)
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// 是否自动重连（仅客户端模式）
    /// Auto reconnect (Client mode only)
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    /// 重连间隔（秒）
    /// Reconnect interval (seconds)
    /// </summary>
    public int ReconnectIntervalSeconds { get; init; } = 5;

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
