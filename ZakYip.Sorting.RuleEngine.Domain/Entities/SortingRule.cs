using System.ComponentModel.DataAnnotations;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 分拣规则实体
/// </summary>
public class SortingRule
{
    /// <summary>
    /// 规则唯一标识
    /// </summary>
    [Required(ErrorMessage = "规则ID不能为空")]
    [StringLength(100, ErrorMessage = "规则ID长度不能超过100个字符")]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(200, ErrorMessage = "规则名称长度不能超过200个字符")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述长度不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 规则优先级（数字越小优先级越高）
    /// </summary>
    [Range(0, 9999, ErrorMessage = "优先级必须在0到9999之间")]
    public int Priority { get; set; }

    /// <summary>
    /// 匹配方法类型
    /// </summary>
    public MatchingMethodType MatchingMethod { get; set; } = MatchingMethodType.LegacyExpression;

    /// <summary>
    /// 规则条件表达式
    /// </summary>
    [Required(ErrorMessage = "条件表达式不能为空")]
    [StringLength(2000, ErrorMessage = "条件表达式长度不能超过2000个字符")]
    public string ConditionExpression { get; set; } = string.Empty;

    /// <summary>
    /// 目标格口号
    /// </summary>
    [Required(ErrorMessage = "目标格口不能为空")]
    [StringLength(100, ErrorMessage = "目标格口长度不能超过100个字符")]
    public string TargetChute { get; set; } = string.Empty;

    /// <summary>
    /// 规则是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
