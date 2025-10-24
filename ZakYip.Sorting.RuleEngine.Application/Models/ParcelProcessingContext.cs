using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Models;

/// <summary>
/// 包裹处理上下文
/// </summary>
public class ParcelProcessingContext
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 小车号
    /// </summary>
    public required string CartNumber { get; init; }
    
    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; init; }
    
    /// <summary>
    /// 序列号
    /// </summary>
    public long SequenceNumber { get; init; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// DWS接收时间
    /// </summary>
    public DateTime? DwsReceivedAt { get; set; }
    
    /// <summary>
    /// DWS数据
    /// </summary>
    public DwsData? DwsData { get; set; }
    
    /// <summary>
    /// 第三方响应
    /// </summary>
    public ThirdPartyResponse? ThirdPartyResponse { get; set; }
}
