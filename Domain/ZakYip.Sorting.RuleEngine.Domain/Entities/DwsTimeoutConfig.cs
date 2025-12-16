namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS数据接收超时配置实体（单例模式）
/// DWS data reception timeout configuration entity (Singleton pattern)
/// </summary>
public record class DwsTimeoutConfig
{
    /// <summary>
    /// 单例配置ID（固定为1）
    /// Singleton configuration ID (Fixed as 1)
    /// </summary>
    public const long SingletonId = 1L;
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public long ConfigId { get; init; } = SingletonId;
    
    /// <summary>
    /// 是否启用超时检查
    /// Enable timeout check
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// 最小等待时间（毫秒）- 避免匹配上一个包裹的DWS数据
    /// Minimum wait time (milliseconds) - Avoid matching DWS data from previous parcel
    /// </summary>
    public required int MinDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 最大等待时间（毫秒）- 超时截止时间
    /// Maximum wait time (milliseconds) - Timeout deadline
    /// </summary>
    public required int MaxDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 异常格口ID - 当DWS数据接收超时时，分配到此格口
    /// Exception chute ID - Assign to this chute when DWS data reception times out
    /// </summary>
    public required long ExceptionChuteId { get; init; }

    /// <summary>
    /// 超时检查间隔（毫秒）- 后台任务检查超时包裹的频率
    /// Timeout check interval (milliseconds) - Frequency of background task checking for timed-out parcels
    /// </summary>
    public required int CheckIntervalMilliseconds { get; init; }
    
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
