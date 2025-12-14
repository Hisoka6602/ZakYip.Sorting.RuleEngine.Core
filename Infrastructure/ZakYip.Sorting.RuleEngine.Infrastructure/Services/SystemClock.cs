using ZakYip.Sorting.RuleEngine.Core.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 系统时钟实现
/// System clock implementation
/// </summary>
public class SystemClock : ISystemClock
{
    /// <summary>
    /// 获取当前本地时间
    /// Get current local time
    /// </summary>
    public DateTime LocalNow => DateTime.Now;

    /// <summary>
    /// 获取当前 UTC 时间
    /// Get current UTC time
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
