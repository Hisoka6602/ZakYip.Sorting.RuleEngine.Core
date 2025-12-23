using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

/// <summary>
/// 空对象模式实现：当分拣机配置禁用时使用
/// Null Object Pattern implementation: used when sorter configuration is disabled
/// </summary>
/// <remarks>
/// 此实现遵循空对象模式（Null Object Pattern），避免在 DI 容器中返回 null。
/// 所有方法都是空操作（no-op），事件永不触发。
/// 
/// This implementation follows the Null Object Pattern to avoid returning null from DI container.
/// All methods are no-ops, events are never triggered.
/// </remarks>
public sealed class NullDownstreamCommunication : IDownstreamCommunication
{
    /// <summary>
    /// 是否已启用（空对象模式始终返回 false）
    /// Whether it is enabled (null object pattern always returns false)
    /// </summary>
    public bool IsEnabled => false;
    /// <summary>
    /// 启动通信（空操作）
    /// Start communication (no-op)
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 空操作：不执行任何操作 / No-op: do nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止通信（空操作）
    /// Stop communication (no-op)
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        // 空操作：不执行任何操作 / No-op: do nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// 广播格口分配（空操作）
    /// Broadcast chute assignment (no-op)
    /// </summary>
    public Task BroadcastChuteAssignmentAsync(string chuteAssignmentJson)
    {
        // 空操作：不执行任何操作 / No-op: do nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// 包裹检测事件（永不触发）
    /// Parcel notification received event (never triggered)
    /// </summary>
#pragma warning disable CS0067 // 事件从不使用（空对象模式设计）/ Event is never used (Null Object Pattern design)
    public event EventHandler<ParcelNotificationReceivedEventArgs>? ParcelNotificationReceived;
#pragma warning restore CS0067

    /// <summary>
    /// 分拣完成事件（永不触发）
    /// Sorting completed received event (never triggered)
    /// </summary>
#pragma warning disable CS0067 // 事件从不使用（空对象模式设计）/ Event is never used (Null Object Pattern design)
    public event EventHandler<SortingCompletedReceivedEventArgs>? SortingCompletedReceived;
#pragma warning restore CS0067

    /// <summary>
    /// 客户端连接事件（永不触发）
    /// Client connected event (never triggered)
    /// </summary>
#pragma warning disable CS0067 // 事件从不使用（空对象模式设计）/ Event is never used (Null Object Pattern design)
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
#pragma warning restore CS0067

    /// <summary>
    /// 客户端断开事件（永不触发）
    /// Client disconnected event (never triggered)
    /// </summary>
#pragma warning disable CS0067 // 事件从不使用（空对象模式设计）/ Event is never used (Null Object Pattern design)
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
#pragma warning restore CS0067
}
