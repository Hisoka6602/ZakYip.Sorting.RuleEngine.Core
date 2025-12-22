namespace ZakYip.Sorting.RuleEngine.Application.Options;

/// <summary>
/// 上游连接配置选项
/// Upstream connection configuration options
/// </summary>
public class ConnectionOptions
{
    /// <summary>
    /// TCP服务器地址（格式：host:port，如 "localhost:8002"）
    /// TCP server address (format: host:port, e.g., "localhost:8002")
    /// </summary>
    public string? TcpServer { get; set; }

    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// TCP配置选项
    /// TCP configuration options
    /// </summary>
    public TcpOptions Tcp { get; set; } = new();

    /// <summary>
    /// 重试次数（0表示不重试）
    /// Retry count (0 means no retry)
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 重试延迟（毫秒）
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// TCP特定配置选项
/// TCP-specific configuration options
/// </summary>
public class TcpOptions
{
    /// <summary>
    /// 接收缓冲区大小（字节）
    /// Receive buffer size in bytes
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 8192;
}
