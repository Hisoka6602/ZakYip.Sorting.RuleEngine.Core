namespace ZakYip.Sorting.RuleEngine.Infrastructure.Models;

/// <summary>
/// 表统计信息
/// </summary>
public class TableStatistics
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    
    /// <summary>
    /// 行数
    /// </summary>
    public long RowCount { get; set; }
    
    /// <summary>
    /// 大小（MB）
    /// </summary>
    public decimal SizeMB { get; set; }
}
