using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 包裹处理状态枚举
/// Parcel processing status enumeration
/// </summary>
public enum ParcelStatus
{
    /// <summary>待处理 / Pending</summary>
    [Description("待处理")]
    Pending = 0,
    
    /// <summary>处理中 / Processing</summary>
    [Description("处理中")]
    Processing = 1,
    
    /// <summary>已完成 / Completed</summary>
    [Description("已完成")]
    Completed = 2,
    
    /// <summary>失败 / Failed</summary>
    [Description("失败")]
    Failed = 3,
    
    /// <summary>超时 / Timeout</summary>
    [Description("超时")]
    Timeout = 4,
    
    /// <summary>丢失 / Lost</summary>
    [Description("丢失")]
    Lost = 5
}
