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
        ArgumentNullException.ThrowIfNull(entity);
        
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
        ArgumentNullException.ThrowIfNull(entity);
        
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
}
