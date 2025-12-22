namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 更新DWS超时配置请求DTO
/// Update DWS timeout configuration request DTO
/// </summary>
public record class DwsTimeoutConfigUpdateRequest
{
    /// <summary>
    /// 是否启用超时检查
    /// Enable timeout check
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; init; }

    /// <summary>
    /// 最小等待时间（毫秒）
    /// Minimum wait time (milliseconds)
    /// </summary>
    /// <example>60</example>
    public required int MinDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 最大等待时间（毫秒）
    /// Maximum wait time (milliseconds)
    /// </summary>
    /// <example>200</example>
    public required int MaxDwsWaitMilliseconds { get; init; }

    /// <summary>
    /// 异常格口ID
    /// Exception chute ID
    /// </summary>
    /// <example>999</example>
    public required long ExceptionChuteId { get; init; }

    /// <summary>
    /// 超时检查间隔（毫秒）
    /// Timeout check interval (milliseconds)
    /// </summary>
    /// <example>100</example>
    public required int CheckIntervalMilliseconds { get; init; }

    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    /// <example>包裹创建后60-200ms内可以绑定DWS数据</example>
    public string? Description { get; init; }
}
