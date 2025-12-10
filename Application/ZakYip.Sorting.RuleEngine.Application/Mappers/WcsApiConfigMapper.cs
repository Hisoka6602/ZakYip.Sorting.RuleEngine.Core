using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// WCS API配置映射器 - 实体与DTO之间的转换
/// WCS API configuration mapper - Conversion between entity and DTO
/// </summary>
public static class WcsApiConfigMapper
{
    /// <summary>
    /// 转换为响应DTO（单例模式，不包含ID）
    /// Convert to response DTO (Singleton pattern, no ID)
    /// </summary>
    public static WcsApiConfigResponseDto ToResponseDto(this WcsApiConfig entity)
    {
        return new WcsApiConfigResponseDto
        {
            ApiName = entity.ApiName,
            BaseUrl = entity.BaseUrl,
            TimeoutSeconds = entity.TimeoutSeconds,
            ApiKey = entity.ApiKey != null ? "******" : null, // 脱敏处理
            CustomHeaders = entity.CustomHeaders,
            HttpMethod = entity.HttpMethod,
            RequestBodyTemplate = entity.RequestBodyTemplate,
            IsEnabled = entity.IsEnabled,
            Priority = entity.Priority,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 从更新请求创建实体
    /// Create entity from update request
    /// </summary>
    public static WcsApiConfig ToEntity(this WcsApiConfigUpdateRequest request)
    {
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

    /// <summary>
    /// 批量转换为响应DTO
    /// Batch convert to response DTO
    /// </summary>
    public static IEnumerable<WcsApiConfigResponseDto> ToResponseDtos(this IEnumerable<WcsApiConfig> entities)
    {
        return entities.Select(ToResponseDto);
    }
}
