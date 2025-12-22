namespace ZakYip.Sorting.RuleEngine.Application.Events.Communication;

/// <summary>
/// 客户端连接/断开事件参数
/// Client connection/disconnection event arguments
/// </summary>
public sealed class ClientConnectionEventArgs : EventArgs
{
    /// <summary>
    /// 客户端唯一标识
    /// Client unique identifier
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// 连接时间
    /// Connection time
    /// </summary>
    public DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    /// 客户端地址（IP:Port）
    /// Client address (IP:Port)
    /// </summary>
    public string? ClientAddress { get; init; }
}
