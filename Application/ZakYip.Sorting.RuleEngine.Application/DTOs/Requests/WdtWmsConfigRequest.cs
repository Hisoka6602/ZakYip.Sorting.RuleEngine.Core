namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 旺店通WMS API配置请求
/// WDT WMS API Configuration Request
/// </summary>
public record WdtWmsConfigRequest
{
    public required string Url { get; init; }
    
    public required string Sid { get; init; }
    
    public required string AppKey { get; init; }
    
    public required string AppSecret { get; init; }
    
    public string Method { get; init; } = "wms.logistics.Consign.weigh";

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeOut { get; init; } = 5000;

    /// <summary>
    /// 表示是否必须包含包装条码
    /// </summary>
    public bool MustIncludeBoxBarcode { get; init; } = false;

    /// <summary>
    /// 默认重量（当无重量数据时使用）
    /// </summary>
    public double DefaultWeight { get; init; } = 0.0;
}
