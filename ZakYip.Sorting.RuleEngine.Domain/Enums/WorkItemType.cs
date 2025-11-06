using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 工作项类型
/// </summary>
public enum WorkItemType
{
    /// <summary>创建包裹</summary>
    [Description("创建包裹")]
    Create,
    
    /// <summary>处理DWS数据</summary>
    [Description("处理DWS数据")]
    ProcessDws
}
