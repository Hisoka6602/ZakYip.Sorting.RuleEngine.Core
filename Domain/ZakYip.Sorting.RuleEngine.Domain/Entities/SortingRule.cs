using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣规则实体
/// </summary>
[SwaggerSchema(Description = "分拣规则实体，定义包裹分拣的匹配条件和目标格口")]
public class SortingRule
{
    /// <summary>
    /// 规则唯一标识
    /// 示例: RULE001
    /// </summary>
    [Required(ErrorMessage = "规则ID不能为空")]
    [StringLength(100, ErrorMessage = "规则ID长度不能超过100个字符")]
    [SwaggerSchema(Description = "规则唯一标识")]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// 示例: 深圳规则
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(200, ErrorMessage = "规则名称长度不能超过200个字符")]
    [SwaggerSchema(Description = "规则名称")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// 示例: 所有发往深圳的包裹
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述长度不能超过500个字符")]
    [SwaggerSchema(Description = "规则描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 规则优先级（数字越小优先级越高）
    /// 示例: 10
    /// </summary>
    [Range(0, 9999, ErrorMessage = "优先级必须在0到9999之间")]
    [SwaggerSchema(Description = "规则优先级(数字越小优先级越高)")]
    public int Priority { get; set; }

    /// <summary>
    /// 匹配方法类型
    /// 示例: LegacyExpression
    /// </summary>
    [SwaggerSchema(Description = "匹配方法类型")]
    public MatchingMethodType MatchingMethod { get; set; } = MatchingMethodType.LegacyExpression;

    /// <summary>
    /// 规则条件表达式
    /// 示例: destination == '深圳'
    /// </summary>
    [Required(ErrorMessage = "条件表达式不能为空")]
    [StringLength(2000, ErrorMessage = "条件表达式长度不能超过2000个字符")]
    [SwaggerSchema(Description = "规则条件表达式")]
    public string ConditionExpression { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口号
    /// 示例: CHUTE01
    /// </summary>
    [Required(ErrorMessage = "目标格口不能为空")]
    [StringLength(100, ErrorMessage = "目标格口长度不能超过100个字符")]
    [SwaggerSchema(Description = "目标格口号")]
    public string TargetChute { get; set; } = string.Empty;

    /// <summary>
    /// 规则是否启用
    /// 示例: true
    /// </summary>
    [SwaggerSchema(Description = "规则是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// 示例: 2023-11-01T08:30:00Z
    /// </summary>
    [SwaggerSchema(Description = "创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// 示例: 2023-11-01T10:15:00Z
    /// </summary>
    [SwaggerSchema(Description = "更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
