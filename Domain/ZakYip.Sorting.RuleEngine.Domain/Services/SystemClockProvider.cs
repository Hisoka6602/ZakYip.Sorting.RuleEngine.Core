namespace ZakYip.Sorting.RuleEngine.Domain.Services;

/// <summary>
/// 系统时钟提供者 - 用于在静态上下文中访问时钟
/// System clock provider - For accessing clock in static contexts
/// </summary>
/// <remarks>
/// ⚠️ 必须在应用程序启动时调用 Initialize() 方法进行初始化
/// ⚠️ Must call Initialize() method during application startup
/// </remarks>
public static class SystemClockProvider
{
    private static volatile Func<DateTime>? _localNowProvider;
    private static volatile Func<DateTime>? _utcNowProvider;
    private static volatile bool _isInitialized;

    /// <summary>
    /// 初始化系统时钟提供者
    /// Initialize system clock provider
    /// </summary>
    /// <param name="localNowProvider">本地时间提供者 / Local time provider</param>
    /// <param name="utcNowProvider">UTC时间提供者 / UTC time provider</param>
    public static void Initialize(Func<DateTime> localNowProvider, Func<DateTime> utcNowProvider)
    {
        _localNowProvider = localNowProvider ?? throw new ArgumentNullException(nameof(localNowProvider));
        _utcNowProvider = utcNowProvider ?? throw new ArgumentNullException(nameof(utcNowProvider));
        _isInitialized = true;
    }
    
    /// <summary>
    /// 检查是否已初始化
    /// Check if initialized
    /// </summary>
    public static bool IsInitialized => _isInitialized;

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
