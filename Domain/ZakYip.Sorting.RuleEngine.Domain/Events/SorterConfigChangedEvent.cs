using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 分拣机配置变更事件 / Sorter Configuration Changed Event
/// </summary>
/// <remarks>
/// 当分拣机配置被更新时触发此事件，用于通知分拣机适配器重新加载配置并重启连接。
/// This event is triggered when Sorter configuration is updated, 
/// notifying the Sorter adapter to reload configuration and restart connections.
/// </remarks>
public readonly record struct SorterConfigChangedEvent : INotification
{
    /// <summary>
    /// 配置ID / Configuration ID
    /// </summary>
    public required long ConfigId { get; init; }
    
    /// <summary>
    /// 配置名称 / Configuration Name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 通信协议类型 (TCP/HTTP/SignalR) / Protocol Type
    /// </summary>
    public required string Protocol { get; init; }
    
    /// <summary>
    /// 连接模式 (Server/Client) / Connection Mode
    /// </summary>
    public required string ConnectionMode { get; init; }
    
    /// <summary>
    /// 主机地址 / Host Address
    /// </summary>
    public required string Host { get; init; }
    
    /// <summary>
    /// 端口号 / Port Number
    /// </summary>
    public required int Port { get; init; }
    
    /// <summary>
    /// 是否启用 / Is Enabled
    /// </summary>
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// 配置更新时间 / Configuration Update Time
    /// </summary>
    public DateTime UpdatedAt { get; init; }
    
    /// <summary>
    /// 变更原因 / Change Reason
    /// </summary>
    public string? Reason { get; init; }
}
