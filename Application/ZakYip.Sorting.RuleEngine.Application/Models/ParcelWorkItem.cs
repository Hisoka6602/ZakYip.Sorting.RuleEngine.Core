using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 工作项，表示需要处理的包裹任务。/ Work item that represents a pending parcel task.
/// </summary>
public class ParcelWorkItem
{
    /// <summary>
    /// 包裹唯一标识。/ Unique identifier of the parcel.
    /// </summary>
    public required string ParcelId { get; init; }

    /// <summary>
    /// 包裹在队列中的顺序号。/ Sequential number of the parcel in the processing queue.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// 工作项类型。/ Type of the work item.
    /// </summary>
    public required WorkItemType WorkType { get; init; }
}
