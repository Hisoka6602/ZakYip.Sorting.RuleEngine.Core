using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 通信类型枚举
/// </summary>
public enum CommunicationType
{
    /// <summary>TCP通信</summary>
    [Description("TCP通信")]
    Tcp = 0,
    
    /// <summary>SignalR通信</summary>
    [Description("SignalR通信")]
    SignalR = 1,
    
    /// <summary>HTTP通信</summary>
    [Description("HTTP通信")]
    Http = 2,
    
    /// <summary>MQTT通信</summary>
    [Description("MQTT通信")]
    Mqtt = 3
}
