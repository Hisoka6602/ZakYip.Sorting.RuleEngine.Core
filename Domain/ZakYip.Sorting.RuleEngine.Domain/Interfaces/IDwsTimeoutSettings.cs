namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS数据接收超时设置接口 / DWS data reception timeout settings interface
/// </summary>
public interface IDwsTimeoutSettings
{
    /// <summary>
    /// 是否启用超时检查 / Enable timeout check
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// 最小等待时间（毫秒）- 避免匹配上一个包裹的DWS数据
    /// Minimum wait time (milliseconds) - Avoid matching DWS data from previous parcel
    /// </summary>
    int MinDwsWaitMilliseconds { get; }

    /// <summary>
    /// 最大等待时间（毫秒）- 超时截止时间
    /// Maximum wait time (milliseconds) - Timeout deadline
    /// </summary>
    int MaxDwsWaitMilliseconds { get; }

    /// <summary>
    /// 异常格口ID - 当DWS数据接收超时时，分配到此格口
    /// Exception chute ID - Assign to this chute when DWS data reception times out
    /// </summary>
    long ExceptionChuteId { get; }

    /// <summary>
    /// 超时检查间隔（毫秒）- 后台任务检查超时包裹的频率
    /// Timeout check interval (milliseconds) - Frequency of background task checking for timed-out parcels
    /// </summary>
    int CheckIntervalMilliseconds { get; }
}
