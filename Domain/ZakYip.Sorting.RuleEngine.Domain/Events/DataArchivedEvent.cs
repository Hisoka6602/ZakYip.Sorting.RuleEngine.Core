using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// 数据归档事件
/// </summary>
public readonly record struct DataArchivedEvent : INotification
{
    /// <summary>
    /// 归档的记录数
    /// </summary>
    public int RecordCount { get; init; }
    
    /// <summary>
    /// 归档的开始日期
    /// </summary>
    public DateTime StartDate { get; init; }
    
    /// <summary>
    /// 归档的结束日期
    /// </summary>
    public DateTime EndDate { get; init; }
    
    /// <summary>
    /// 归档执行时间
    /// </summary>
    public DateTime ArchivedAt { get; init; }
    
    /// <summary>
    /// 归档耗时（毫秒）
    /// </summary>
    public long DurationMs { get; init; }
}
