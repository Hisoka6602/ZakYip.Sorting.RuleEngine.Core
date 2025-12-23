using ZakYip.Sorting.RuleEngine.Domain.Services;
using MediatR;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// DWS数据接收事件
/// </summary>
public record class DwsDataReceivedEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// DWS数据
    /// </summary>
    public required DwsData DwsData { get; init; }
    
    /// <summary>
    /// 接收时间
    /// </summary>
    public DateTime ReceivedAt { get; init; } = SystemClockProvider.LocalNow;

    /// <summary>
    /// DWS设备地址或连接信息（IP:Port或设备ID）
    /// DWS device address or connection info (IP:Port or device ID)
    /// </summary>
    public string? SourceAddress { get; init; }
}
