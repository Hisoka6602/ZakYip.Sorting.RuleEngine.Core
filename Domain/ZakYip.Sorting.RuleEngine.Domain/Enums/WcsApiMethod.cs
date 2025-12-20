namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// WCS API 可测试方法枚举
/// WCS API testable methods enumeration
/// </summary>
public enum WcsApiMethod
{
    /// <summary>
    /// 扫描包裹 - ScanParcelAsync
    /// Scan parcel
    /// </summary>
    ScanParcel = 1,

    /// <summary>
    /// 请求格口 - RequestChuteAsync
    /// Request chute
    /// </summary>
    RequestChute = 2,

    /// <summary>
    /// 落格回调 - NotifyChuteLandingAsync
    /// Notify chute landing
    /// </summary>
    NotifyChuteLanding = 3
}
