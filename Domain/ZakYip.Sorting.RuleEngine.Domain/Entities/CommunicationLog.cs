using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;
/// <summary>
/// 通信日志实体
/// </summary>
public class CommunicationLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public long Id { get; set; }
    
    /// 通信类型
    public CommunicationType CommunicationType { get; set; }
    /// 通信方向
    public CommunicationDirection Direction { get; set; }
    /// 包裹ID
    public string? ParcelId { get; set; }
    /// 消息内容
    public string Message { get; set; } = string.Empty;
    /// 远程地址
    public string? RemoteAddress { get; set; }
    /// 是否成功
    public bool IsSuccess { get; set; }
    /// 错误信息
    public string? ErrorMessage { get; set; }
    /// 创建时间
    public DateTime CreatedAt { get; set; } = SystemClockProvider.LocalNow;
}
