using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// WCS API配置映射器 - 实体与DTO之间的转换
/// WCS API configuration mapper - Conversion between entity and DTO
/// </summary>
public static class WcsApiConfigMapper
{
    /// <summary>
    /// 从更新请求创建实体
    /// Create entity from update request
    /// </summary>
    public static WcsApiConfig ToEntity(this WcsApiConfigUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var now = DateTime.Now;
        return new WcsApiConfig
        {
            ConfigId = WcsApiConfig.SINGLETON_ID,
            ApiName = request.ApiName,
            BaseUrl = request.BaseUrl,
            TimeoutSeconds = request.TimeoutSeconds,
            ApiKey = request.ApiKey,
            CustomHeaders = request.CustomHeaders,
            HttpMethod = request.HttpMethod,
            RequestBodyTemplate = request.RequestBodyTemplate,
            IsEnabled = request.IsEnabled,
            Priority = request.Priority,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
