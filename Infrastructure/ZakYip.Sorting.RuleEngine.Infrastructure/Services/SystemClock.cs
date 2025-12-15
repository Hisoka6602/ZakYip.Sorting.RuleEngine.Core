using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

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
#pragma warning disable RS0030 // Banned API - This is the designated encapsulation class for DateTime.Now
    public DateTime LocalNow => DateTime.Now;
#pragma warning restore RS0030

    /// <summary>
    /// 获取当前 UTC 时间
    /// Get current UTC time
    /// </summary>
#pragma warning disable RS0030 // Banned API - This is the designated encapsulation class for DateTime.UtcNow
    public DateTime UtcNow => DateTime.UtcNow;
#pragma warning restore RS0030
}
