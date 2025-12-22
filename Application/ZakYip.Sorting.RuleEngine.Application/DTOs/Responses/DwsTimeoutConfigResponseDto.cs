namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// DWS包裹绑定超时配置响应DTO
/// DWS parcel binding timeout configuration response DTO
/// </summary>
public record class DwsTimeoutConfigResponseDto
{
    /// <summary>
    /// 是否启用超时检查
    /// Enable timeout check
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; init; }

    /// <summary>
    /// 最小等待时间（毫秒）- 包裹创建后的最小等待时间
    /// Minimum wait time (milliseconds) - Minimum wait time after parcel creation
    /// </summary>
    /// <example>60</example>
    public required int MinDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 最大等待时间（毫秒）- 包裹创建后的超时时间
    /// Maximum wait time (milliseconds) - Timeout after parcel creation
    /// </summary>
    /// <example>200</example>
    public required int MaxDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 异常格口ID - 超时包裹分配的目标格口
    /// Exception chute ID - Target chute for timeout parcels
    /// </summary>
    /// <example>999</example>
    public required long ExceptionChuteId { get; init; }

    /// <summary>
    /// 超时检查间隔（毫秒）- 后台任务检查超时包裹的频率
    /// Timeout check interval (milliseconds) - Frequency of background task checking
    /// </summary>
    /// <example>100</example>
    public required int CheckIntervalMilliseconds { get; init; }

    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    /// <example>包裹创建后60-200ms内可以绑定DWS数据</example>
    public string? Description { get; init; }

    /// <summary>
    /// 最后更新时间
    /// Last updated time
    /// </summary>
    public required DateTime UpdatedAt { get; init; }
}
