using ZakYip.Sorting.RuleEngine.Domain.Enums;

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
    
    /// <summary>
    /// 通信类型
    /// </summary>
    public CommunicationType CommunicationType { get; set; }
    
    /// <summary>
    /// 通信方向
    /// </summary>
    public CommunicationDirection Direction { get; set; }
    
    /// <summary>
    /// 包裹ID
    /// </summary>
    public string? ParcelId { get; set; }
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 远程地址
    /// </summary>
    public string? RemoteAddress { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
