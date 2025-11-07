namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 缓存配置
/// Cache settings
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// 绝对过期时间（秒），默认3600秒（1小时）
    /// Absolute expiration time in seconds, default 3600 (1 hour)
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 3600;
    
    /// <summary>
    /// 滑动过期时间（秒），默认600秒（10分钟）
    /// Sliding expiration time in seconds, default 600 (10 minutes)
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 600;
}
