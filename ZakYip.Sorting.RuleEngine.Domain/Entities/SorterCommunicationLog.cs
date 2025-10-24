namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣机通信日志实体
/// </summary>
public class SorterCommunicationLog
{
    /// <summary>
    /// 日志ID（自增主键）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 分拣机地址
    /// </summary>
    public string SorterAddress { get; set; } = string.Empty;

    /// <summary>
    /// 通信类型（接收/发送）
    /// </summary>
    public string CommunicationType { get; set; } = string.Empty;

    /// <summary>
    /// 原始内容
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// 格式化内容
    /// </summary>
    public string? FormattedContent { get; set; }

    /// <summary>
    /// 提取的包裹ID（如果是发送则为空）
    /// </summary>
    public string? ExtractedParcelId { get; set; }

    /// <summary>
    /// 提取的小车号（如果是发送则为空）
    /// </summary>
    public string? ExtractedCartNumber { get; set; }

    /// <summary>
    /// 通信时间
    /// </summary>
    public DateTime CommunicationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
