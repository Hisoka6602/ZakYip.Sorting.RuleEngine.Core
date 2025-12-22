namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 包裹最终状态（分拣完成后的状态）
/// Parcel final status (status after sorting completion)
/// </summary>
public enum ParcelFinalStatus
{
    /// <summary>
    /// 成功落格
    /// Successfully sorted to chute
    /// </summary>
    Success,

    /// <summary>
    /// 超时未落格
    /// Timeout - not sorted to chute
    /// </summary>
    Timeout,

    /// <summary>
    /// 包裹丢失（未检测到落格）
    /// Parcel lost (sorting not detected)
    /// </summary>
    Lost,

    /// <summary>
    /// 执行错误
    /// Execution error
    /// </summary>
    ExecutionError
}
