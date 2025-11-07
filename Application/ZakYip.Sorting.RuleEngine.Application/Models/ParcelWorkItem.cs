using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 工作项
/// </summary>
public class ParcelWorkItem
{
    public required string ParcelId { get; init; }
    public long SequenceNumber { get; init; }
    public required WorkItemType WorkType { get; init; }
}
