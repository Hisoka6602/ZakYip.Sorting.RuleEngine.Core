namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// API请求状态枚举
/// API request status enumeration
/// </summary>
public enum ApiRequestStatus
{
    /// <summary>
    /// 成功 / Success
    /// </summary>
    Success = 0,

    /// <summary>
    /// 失败 / Failure
    /// </summary>
    Failure = 1,

    /// <summary>
    /// 异常 / Exception
    /// </summary>
    Exception = 2,

    /// <summary>
    /// 超时 / Timeout
    /// </summary>
    Timeout = 3
}
