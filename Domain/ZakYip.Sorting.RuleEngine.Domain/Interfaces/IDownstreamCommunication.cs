namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 下游通信接口（与WheelDiverterSorter通信）
/// Downstream communication interface (communicate with WheelDiverterSorter)
/// </summary>
/// <remarks>
/// 支持双模式：
/// - Server模式：RuleEngine作为服务器，WheelDiverterSorter主动连接
/// - Client模式：RuleEngine作为客户端，主动连接到WheelDiverterSorter
/// 
/// Supports dual modes:
/// - Server mode: RuleEngine acts as server, WheelDiverterSorter actively connects
/// - Client mode: RuleEngine acts as client, actively connects to WheelDiverterSorter
/// </remarks>
public interface IDownstreamCommunication
{
    /// <summary>
    /// 启动通信（Server模式：开始监听；Client模式：连接到服务器）
    /// Start communication (Server mode: start listening; Client mode: connect to server)
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止通信
    /// Stop communication
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播格口分配通知给所有连接的客户端（Server模式）或发送给服务器（Client模式）
    /// Broadcast chute assignment to all connected clients (Server mode) or send to server (Client mode)
    /// </summary>
    /// <param name="chuteAssignmentJson">格口分配通知的JSON字符串 / JSON string of chute assignment notification</param>
    Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson);

    /// <summary>
    /// 包裹检测通知接收事件（从WheelDiverterSorter接收）
    /// Parcel detected notification received event (received from WheelDiverterSorter)
    /// </summary>
    event EventHandler<Domain.Events.ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived;

    /// <summary>
    /// 分拣完成通知接收事件（从WheelDiverterSorter接收）
    /// Sorting completed notification received event (received from WheelDiverterSorter)
    /// </summary>
    event EventHandler<Domain.Events.SortingCompletedReceivedEventArgs>? SortingCompletedReceived;

    /// <summary>
    /// 客户端连接事件（仅Server模式）
    /// Client connected event (Server mode only)
    /// </summary>
    event EventHandler<Domain.Events.ClientConnectionEventArgs>? ClientConnected;

    /// <summary>
    /// 客户端断开事件（仅Server模式）
    /// Client disconnected event (Server mode only)
    /// </summary>
    event EventHandler<Domain.Events.ClientConnectionEventArgs>? ClientDisconnected;
}
