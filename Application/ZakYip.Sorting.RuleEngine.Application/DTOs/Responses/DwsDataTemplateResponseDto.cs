namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// DWS数据模板响应DTO（单例模式，不包含ID）
/// DWS data template response DTO (Singleton pattern, no ID)
/// </summary>
public record DwsDataTemplateResponseDto
{
    public required string Name { get; init; }
    public required string Template { get; init; }
    public string Delimiter { get; init; } = ",";
    public bool IsJsonFormat { get; init; }
    public required bool IsEnabled { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
