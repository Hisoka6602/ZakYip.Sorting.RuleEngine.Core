namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 客户端连接/断开事件参数
/// Client connection/disconnection event arguments
/// </summary>
/// <example>
/// <code>
/// tcpServer.ClientConnected += (sender, e) =>
/// {
///     Console.WriteLine($"客户端 {e.ClientId} 从 {e.ClientAddress} 连接");
/// };
/// </code>
/// </example>
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
    public required DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    /// 客户端地址（IP:Port）
    /// Client address (IP:Port)
    /// </summary>
    public required string ClientAddress { get; init; }
}
