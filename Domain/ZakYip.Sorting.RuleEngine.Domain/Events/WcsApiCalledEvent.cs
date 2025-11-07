using MediatR;

namespace ZakYip.Sorting.RuleEngine.Domain.Events;

/// <summary>
/// WCS API调用事件
/// </summary>
public record struct WcsApiCalledEvent : INotification
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// API地址
    /// </summary>
    public required string ApiUrl { get; init; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }
    
    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? StatusCode { get; init; }
    
    /// <summary>
    /// 调用耗时（毫秒）
    /// </summary>
    public long DurationMs { get; init; }
    
    /// <summary>
    /// 调用时间
    /// </summary>
    public DateTime CalledAt { get; init; }
    
    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }
}
