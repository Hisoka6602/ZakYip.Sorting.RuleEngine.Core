using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

/// <summary>
/// DWS数据接收超时配置 / DWS data reception timeout settings
/// </summary>
public class DwsTimeoutSettings : IDwsTimeoutSettings
{
    /// <summary>
    /// 最小等待时间（秒）- 避免匹配上一个包裹的DWS数据
    /// Minimum wait time (seconds) - Avoid matching DWS data from previous parcel
    /// </summary>
    public int MinDwsWaitSeconds { get; set; } = 2;

    /// <summary>
    /// 最大等待时间（秒）- 超时截止时间
    /// Maximum wait time (seconds) - Timeout deadline
    /// </summary>
    public int MaxDwsWaitSeconds { get; set; } = 30;

    /// <summary>
    /// 异常格口ID - 当DWS数据接收超时时，分配到此格口
    /// Exception chute ID - Assign to this chute when DWS data reception times out
    /// </summary>
    public long ExceptionChuteId { get; set; } = 0;

    /// <summary>
    /// 是否启用超时检查
    /// Enable timeout check
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 超时检查间隔（秒）- 后台任务检查超时包裹的频率
    /// Timeout check interval (seconds) - Frequency of background task checking for timed-out parcels
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 5;
}
