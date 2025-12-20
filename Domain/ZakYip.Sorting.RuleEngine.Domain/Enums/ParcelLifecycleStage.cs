using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 包裹生命周期阶段枚举
/// Parcel lifecycle stage enumeration
/// </summary>
public enum ParcelLifecycleStage
{
    /// <summary>已创建 / Created</summary>
    [Description("已创建")]
    Created = 0,
    
    /// <summary>已接收DWS信息 / DWS data received</summary>
    [Description("已接收DWS信息")]
    DwsReceived = 1,
    
    /// <summary>已请求API / API requested</summary>
    [Description("已请求API")]
    ApiRequested = 2,
    
    /// <summary>已分配格口 / Chute assigned</summary>
    [Description("已分配格口")]
    ChuteAssigned = 3,
    
    /// <summary>已落格 / Landed</summary>
    [Description("已落格")]
    Landed = 4,
    
    /// <summary>已集包 / Bagged</summary>
    [Description("已集包")]
    Bagged = 5,
    
    /// <summary>已完成 / Completed</summary>
    [Description("已完成")]
    Completed = 6,
    
    /// <summary>超时 / Timeout</summary>
    [Description("超时")]
    Timeout = 97,
    
    /// <summary>丢失 / Lost</summary>
    [Description("丢失")]
    Lost = 98,
    
    /// <summary>异常 / Error</summary>
    [Description("异常")]
    Error = 99
}
