namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS数据解析模板（单例模式）
/// DWS data parsing template (Singleton pattern)
/// </summary>
public record class DwsDataTemplate
{
    /// <summary>
    /// 单例模板ID（固定为1）
    /// Singleton template ID (Fixed as 1)
    /// </summary>
    public const long SingletonId = 1L;
    
    /// 模板ID（主键）- 内部使用
    /// Template ID (Primary Key) - Internal use only
    public long TemplateId { get; init; } = SingletonId;
    /// 模板名称
    /// Template name
    public required string Name { get; init; }
    /// 数据模板格式，使用占位符定义字段位置
    /// 示例：{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
    /// Data template format, using placeholders to define field positions
    /// Example: {Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}
    public required string Template { get; init; }
    /// 字段分隔符，默认为逗号
    /// Field delimiter, default is comma
    public string Delimiter { get; init; } = ",";
    /// 是否为JSON格式，true时忽略模板直接解析JSON
    /// Is JSON format, when true ignores template and parses JSON directly
    public bool IsJsonFormat { get; init; }
    /// 是否启用
    /// Is enabled
    public required bool IsEnabled { get; init; }
    /// 备注说明
    /// Description
    public string? Description { get; init; }
    /// 创建时间
    /// Created time
    public required DateTime CreatedAt { get; init; }
    /// 最后更新时间
    /// Last updated time
    public required DateTime UpdatedAt { get; init; }
}
