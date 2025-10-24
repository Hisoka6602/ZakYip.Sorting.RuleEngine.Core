namespace ZakYip.Sorting.RuleEngine.Infrastructure.Models;

/// <summary>
/// 索引使用信息
/// </summary>
public class IndexUsageInfo
{
    /// <summary>
    /// 数据库名
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
    
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// 索引名
    /// </summary>
    public string IndexName { get; set; } = string.Empty;
}
