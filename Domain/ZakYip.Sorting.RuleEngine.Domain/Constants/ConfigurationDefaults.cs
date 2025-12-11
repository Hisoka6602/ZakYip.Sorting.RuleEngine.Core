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
