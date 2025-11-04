using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Mappers;

/// <summary>
/// 实体到响应DTO的映射器
/// </summary>
public static class EntityToDtoMapper
{
    /// <summary>
    /// 将SortingRule实体映射为SortingRuleResponseDto
    /// </summary>
    public static SortingRuleResponseDto ToResponseDto(this SortingRule entity)
    {
        return new SortingRuleResponseDto
        {
            RuleId = entity.RuleId,
            RuleName = entity.RuleName,
            Description = entity.Description,
            MatchingMethod = entity.MatchingMethod.ToString(),
            ConditionExpression = entity.ConditionExpression,
            TargetChute = entity.TargetChute,
            Priority = entity.Priority,
            IsEnabled = entity.IsEnabled,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将SortingRule实体集合映射为SortingRuleResponseDto集合
    /// </summary>
    public static IEnumerable<SortingRuleResponseDto> ToResponseDtos(this IEnumerable<SortingRule> entities)
    {
        return entities.Select(e => e.ToResponseDto());
    }

    /// <summary>
    /// 将Chute实体映射为ChuteResponseDto
    /// </summary>
    public static ChuteResponseDto ToResponseDto(this Chute entity)
    {
        return new ChuteResponseDto
        {
            ChuteId = entity.ChuteId,
            ChuteName = entity.ChuteName,
            ChuteCode = entity.ChuteCode,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将Chute实体集合映射为ChuteResponseDto集合
    /// </summary>
    public static IEnumerable<ChuteResponseDto> ToResponseDtos(this IEnumerable<Chute> entities)
    {
        return entities.Select(e => e.ToResponseDto());
    }

    /// <summary>
    /// 将ThirdPartyApiConfig实体映射为ThirdPartyApiConfigResponseDto（脱敏API密钥）
    /// </summary>
    public static ThirdPartyApiConfigResponseDto ToResponseDto(this ThirdPartyApiConfig entity)
    {
        return new ThirdPartyApiConfigResponseDto
        {
            ConfigId = entity.ConfigId,
            Name = entity.ApiName,
            BaseUrl = entity.BaseUrl,
            // 脱敏处理：只显示前3位和后3位，中间用*替代
            ApiKey = MaskSensitiveData(entity.ApiKey),
            TimeoutSeconds = entity.TimeoutSeconds,
            Priority = entity.Priority,
            IsEnabled = entity.IsEnabled,
            HttpMethod = entity.HttpMethod,
            CustomHeaders = entity.CustomHeaders,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将ThirdPartyApiConfig实体集合映射为ThirdPartyApiConfigResponseDto集合
    /// </summary>
    public static IEnumerable<ThirdPartyApiConfigResponseDto> ToResponseDtos(this IEnumerable<ThirdPartyApiConfig> entities)
    {
        return entities.Select(e => e.ToResponseDto());
    }

    /// <summary>
    /// 脱敏敏感数据
    /// </summary>
    private static string? MaskSensitiveData(string? data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        if (data.Length <= 6)
        {
            return new string('*', data.Length);
        }

        var prefix = data.Substring(0, 3);
        var suffix = data.Substring(data.Length - 3);
        var masked = new string('*', data.Length - 6);

        return $"{prefix}{masked}{suffix}";
    }
}
