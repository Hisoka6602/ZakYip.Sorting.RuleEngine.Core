namespace ZakYip.Sorting.RuleEngine.Domain.Constants;

/// <summary>
/// 对象池相关默认值
/// Object pool defaults
/// </summary>
public static class ObjectPoolDefaults
{
    /// <summary>
    /// Stopwatch对象池最大大小
    /// Maximum size for Stopwatch object pool
    /// </summary>
    public const int StopwatchPoolSize = 100;
}

/// <summary>
/// 分页相关默认值
/// Pagination defaults
/// </summary>
public static class PaginationDefaults
{
    /// <summary>
    /// 默认页大小
    /// Default page size
    /// </summary>
    public const int DefaultPageSize = 20;
    
    /// <summary>
    /// 最大记录数
    /// Maximum records
    /// </summary>
    public const int MaxRecords = 1000;
}

/// <summary>
/// 图片文件相关默认值
/// Image file defaults
/// </summary>
public static class ImageFileDefaults
{
    /// <summary>
    /// 默认图片内容类型
    /// Default image content type
    /// </summary>
    public const string DefaultContentType = "image/jpeg";
}

/// <summary>
/// 配置变更原因常量
/// Configuration change reason constants
/// </summary>
public static class ConfigChangeReasons
{
    /// <summary>
    /// 配置已创建 / Configuration created
    /// </summary>
    public const string ConfigurationCreated = "Configuration created";
    
    /// <summary>
    /// 配置已更新 / Configuration updated
    /// </summary>
    public const string ConfigurationUpdated = "Configuration updated";
    
    /// <summary>
    /// 手动重载触发 / Manual reload triggered
    /// </summary>
    public const string ManualReloadTriggered = "Manual reload triggered";
    
    /// <summary>
    /// 用户更新 / User update
    /// </summary>
    public const string UserUpdate = "User update";
}
