using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 包裹处理状态枚举
/// </summary>
public enum ParcelStatus
{
    /// <summary>待处理</summary>
    [Description("待处理")]
    Pending = 0,
    
    /// <summary>处理中</summary>
    [Description("处理中")]
    Processing = 1,
    
    /// <summary>已完成</summary>
    [Description("已完成")]
    Completed = 2,
    
    /// <summary>失败</summary>
    [Description("失败")]
    Failed = 3
}
