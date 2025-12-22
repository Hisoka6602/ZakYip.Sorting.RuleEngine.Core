namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 分拣模式枚举
/// Sorting mode enumeration
/// </summary>
public enum SortingMode
{
    /// <summary>
    /// 未指定（默认）
    /// Unspecified (default)
    /// </summary>
    Unspecified = 0,
    
    /// <summary>
    /// 规则分拣模式 - 使用规则引擎匹配格口
    /// Rule sorting mode - Uses rule engine to match chutes
    /// </summary>
    RuleBased = 1,
    
    /// <summary>
    /// 自动应答模式 - 从配置的格口数组中随机选择
    /// Auto-response mode - Randomly selects from configured chute array
    /// </summary>
    AutoResponse = 2,
    
    /// <summary>
    /// API模式 - 通过外部API（如WCS）获取格口
    /// API mode - Obtains chute through external API (e.g., WCS)
    /// </summary>
    ApiDriven = 3
}
