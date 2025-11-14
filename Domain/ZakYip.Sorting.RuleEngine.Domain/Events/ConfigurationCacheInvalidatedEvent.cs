using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 配置缓存失效事件
/// </summary>
public readonly record struct ConfigurationCacheInvalidatedEvent : INotification
{
    /// <summary>
    /// 缓存类型
    /// </summary>
    public required string CacheType { get; init; }
    
    /// <summary>
    /// 失效原因
    /// </summary>
    public required string Reason { get; init; }
    
    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTime InvalidatedAt { get; init; }
}
