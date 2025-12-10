namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS数据解析模板
/// DWS data parsing template
/// </summary>
public record class DwsDataTemplate
{
    /// <summary>
    /// 模板ID（主键）
    /// Template ID (Primary Key)
    /// </summary>
    public required string TemplateId { get; init; }

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
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// Last updated time
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
}
