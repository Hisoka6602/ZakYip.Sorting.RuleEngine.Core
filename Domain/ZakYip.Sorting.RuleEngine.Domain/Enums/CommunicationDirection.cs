using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 通信方向枚举
/// </summary>
public enum CommunicationDirection
{
    /// <summary>入站（接收）</summary>
    [Description("入站")]
    Inbound = 0,
    
    /// <summary>出站（发送）</summary>
    [Description("出站")]
    Outbound = 1
}
