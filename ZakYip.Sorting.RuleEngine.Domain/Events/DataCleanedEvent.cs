using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 数据清理事件
/// </summary>
public record struct DataCleanedEvent : INotification
{
    /// <summary>
    /// 清理的记录数
    /// </summary>
    public int RecordCount { get; init; }
    
    /// <summary>
    /// 清理的表名
    /// </summary>
    public required string TableName { get; init; }
    
    /// <summary>
    /// 清理的截止日期
    /// </summary>
    public DateTime CutoffDate { get; init; }
    
    /// <summary>
    /// 清理执行时间
    /// </summary>
    public DateTime CleanedAt { get; init; }
    
    /// <summary>
    /// 清理耗时（毫秒）
    /// </summary>
    public long DurationMs { get; init; }
}
