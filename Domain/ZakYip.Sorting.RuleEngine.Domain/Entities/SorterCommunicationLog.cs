using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 通信类型（TCP/SignalR/HTTP/MQTT）
    public CommunicationType CommunicationType { get; set; }
    /// 分拣机地址
    public string SorterAddress { get; set; } = string.Empty;
    /// 原始内容
    public string OriginalContent { get; set; } = string.Empty;
    /// 格式化内容
    public string? FormattedContent { get; set; }
    /// 提取的包裹ID（如果是发送则为空）
    public string? ExtractedParcelId { get; set; }
    /// 提取的小车号（如果是发送则为空）
    public string? ExtractedCartNumber { get; set; }
    /// 通信时间
    public DateTime CommunicationTime { get; set; } = SystemClockProvider.LocalNow;
    /// 是否成功
    public bool IsSuccess { get; set; }
    /// 错误信息
    public string? ErrorMessage { get; set; }
}
