namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// WCS适配器配置更新请求
/// WCS adapter configuration update request
/// </summary>
public record class WcsConfigUpdateRequest
{
    /// <summary>
    /// 激活的适配器名称
    /// Active adapter name
    /// </summary>
    /// <example>JushuitanErpApiClient</example>
    /// <remarks>
    /// 可选值 / Available options:
    /// - WcsApiClient (标准WCS API / Standard WCS API)
    /// - JushuitanErpApiClient (聚水潭ERP / Jushuituan ERP)
    /// - WdtWmsApiClient (旺店通WMS / WDT WMS)
    /// - WdtErpFlagshipApiClient (旺店通旗舰版ERP / WDT Flagship ERP)
    /// - PostCollectionApiClient (邮政揽收 / Post Collection)
    /// - PostProcessingCenterApiClient (邮政处理中心 / Post Processing Center)
    /// - MockWcsApiAdapter (模拟适配器 / Mock Adapter)
    /// </remarks>
    public required string ActiveAdapter { get; init; }
    
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
    /// <example>切换到聚水潭ERP适配器</example>
    public string? Description { get; init; }
}
