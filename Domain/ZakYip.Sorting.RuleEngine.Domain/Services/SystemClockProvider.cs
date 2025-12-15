namespace ZakYip.Sorting.RuleEngine.Domain.Services;

/// <summary>
/// 系统时钟提供者 - 用于在静态上下文中访问时钟
/// System clock provider - For accessing clock in static contexts
/// </summary>
public static class SystemClockProvider
{
    private static Func<DateTime>? _localNowProvider;
    private static Func<DateTime>? _utcNowProvider;

    /// <summary>
    /// 初始化系统时钟提供者
    /// Initialize system clock provider
    /// </summary>
    /// <param name="localNowProvider">本地时间提供者 / Local time provider</param>
    /// <param name="utcNowProvider">UTC时间提供者 / UTC time provider</param>
    public static void Initialize(Func<DateTime> localNowProvider, Func<DateTime> utcNowProvider)
    {
        _localNowProvider = localNowProvider;
        _utcNowProvider = utcNowProvider;
    }

    /// <summary>
    /// 获取当前本地时间
    /// Get current local time
    /// </summary>
#pragma warning disable RS0030 // Banned API - This is the designated encapsulation class for DateTime.Now
    public static DateTime LocalNow => _localNowProvider?.Invoke() ?? DateTime.Now;
#pragma warning restore RS0030

    /// <summary>
    /// 获取当前UTC时间
    /// Get current UTC time
    /// </summary>
#pragma warning disable RS0030 // Banned API - This is the designated encapsulation class for DateTime.UtcNow
    public static DateTime UtcNow => _utcNowProvider?.Invoke() ?? DateTime.UtcNow;
#pragma warning restore RS0030
}
