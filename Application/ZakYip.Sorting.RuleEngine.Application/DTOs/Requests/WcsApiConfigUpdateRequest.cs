namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// WCS API配置更新请求
/// WCS API configuration update request
/// </summary>
public record class WcsApiConfigUpdateRequest
{
    /// <summary>
    /// 激活的WCS API适配器类型
    /// Active WCS API adapter type
    /// </summary>
    /// <example>WcsApiClient</example>
    /// <remarks>
    /// 可选值 / Available options: 
    /// - WcsApiClient (标准WCS API / Standard WCS API)
    /// - JushuitanErpApiClient (聚水潭ERP / Jushuituan ERP)
    /// - WdtWmsApiClient (旺店通WMS / WDT WMS)
    /// - WdtErpFlagshipApiClient (旺店通旗舰版ERP / WDT Flagship ERP)
    /// - PostCollectionApiClient (邮政揽收 / Post Collection)
    /// - PostProcessingCenterApiClient (邮政处理中心 / Post Processing Center)
    /// </remarks>
    public required string ActiveAdapterType { get; init; }
    
    /// <summary>
    /// API接口URL
    /// API endpoint URL
    /// </summary>
    /// <example>http://localhost:8080/api/wcs</example>
    public required string Url { get; init; }
    
    /// <summary>
    /// API密钥（可选）
    /// API Key (optional)
    /// </summary>
    /// <example>your-api-key-here</example>
    public string? ApiKey { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    /// <example>30000</example>
    public int TimeoutMs { get; init; } = 30000;
    
    /// <summary>
    /// 是否禁用SSL验证（仅用于开发/测试环境）
    /// Disable SSL validation (for development/testing only)
    /// </summary>
    /// <example>false</example>
    public bool DisableSslValidation { get; init; }
    
    /// <summary>
    /// 是否启用
    /// Is enabled
    /// </summary>
    /// <example>true</example>
    public required bool IsEnabled { get; init; }
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    /// <example>生产环境WCS配置</example>
    public string? Description { get; init; }
}
