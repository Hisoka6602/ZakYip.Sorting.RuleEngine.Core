namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 旺店通WMS API配置请求
/// WDT WMS API Configuration Request
/// </summary>
public class WdtWmsConfigRequest
{
    public string Url { get; set; } = string.Empty;
    
    public string Sid { get; set; } = string.Empty;
    
    public string AppKey { get; set; } = string.Empty;
    
    public string AppSecret { get; set; } = string.Empty;
    
    public string Method { get; set; } = "wms.logistics.Consign.weigh";

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeOut { get; set; } = 5000;

    /// <summary>
    /// 表示是否必须包含包装条码
    /// </summary>
    public bool MustIncludeBoxBarcode { get; set; } = false;

    /// <summary>
    /// 默认重量（当无重量数据时使用）
    /// </summary>
    public double DefaultWeight { get; set; } = 0.0;
}
