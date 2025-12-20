using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 位置偏向枚举
/// Position bias enumeration
/// </summary>
public enum PositionBias
{
    /// <summary>左侧 / Left</summary>
    [Description("左侧")]
    Left = 0,
    
    /// <summary>中间 / Center</summary>
    [Description("中间")]
    Center = 1,
    
    /// <summary>右侧 / Right</summary>
    [Description("右侧")]
    Right = 2,
    
    /// <summary>未指定 / Unspecified</summary>
    [Description("未指定")]
    Unspecified = 99
}
