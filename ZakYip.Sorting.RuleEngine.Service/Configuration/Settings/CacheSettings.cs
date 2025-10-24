namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// 缓存配置
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// 绝对过期时间（秒），默认3600秒（1小时）
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 3600;
    
    /// <summary>
    /// 滑动过期时间（秒），默认600秒（10分钟）
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 600;
}
