using ZakYip.Sorting.RuleEngine.Application.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 工作项
/// </summary>
public class ParcelWorkItem
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 序列号
    /// </summary>
    public long SequenceNumber { get; init; }
    
    /// <summary>
    /// 工作项类型
    /// </summary>
    public required WorkItemType WorkType { get; init; }
}
