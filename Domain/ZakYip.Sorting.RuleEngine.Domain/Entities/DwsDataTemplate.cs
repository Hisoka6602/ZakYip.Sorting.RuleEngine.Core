namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS数据解析模板（单例模式）
/// DWS data parsing template (Singleton pattern)
/// </summary>
public record class DwsDataTemplate
{
    /// <summary>
    /// 单例模板ID（固定为1，不对外暴露）
    /// Singleton template ID (Fixed as 1, not exposed externally)
    /// </summary>
    internal const long SINGLETON_ID = 1L;
    
    /// <summary>
    /// 模板ID（主键）- 内部使用
    /// Template ID (Primary Key) - Internal use only
    /// </summary>
    public long TemplateId { get; init; } = SINGLETON_ID;

    /// <summary>
    /// 模板名称
    /// Template name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 数据模板格式，使用占位符定义字段位置
    /// 示例：{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
    /// Data template format, using placeholders to define field positions
    /// Example: {Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// 字段分隔符，默认为逗号
    /// Field delimiter, default is comma
    /// </summary>
    public string Delimiter { get; init; } = ",";

    /// <summary>
    /// 是否为JSON格式，true时忽略模板直接解析JSON
    /// Is JSON format, when true ignores template and parses JSON directly
    /// </summary>
    public bool IsJsonFormat { get; init; }

    /// <summary>
    /// 是否启用
    /// Is enabled
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 创建时间
    /// Created time
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// Last updated time
    /// </summary>
    public required DateTime UpdatedAt { get; init; }
}
