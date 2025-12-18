namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 旺店通WMS API配置请求
/// WDT WMS API Configuration Request
/// </summary>
public record WdtWmsConfigRequest
{
    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; set; }
    
    public required string Url { get; set; }
    
    public required string Sid { get; set; }
    
    public required string AppKey { get; set; }
    
    public required string AppSecret { get; set; }
    
    public string Method { get; set; } = "wms.logistics.Consign.weigh";

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 表示是否必须包含包装条码
    /// </summary>
    public bool MustIncludeBoxBarcode { get; set; } = false;

    /// <summary>
    /// 默认重量（当无重量数据时使用）
    /// </summary>
    public double DefaultWeight { get; set; } = 0.0;
    
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; set; }
}
