namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// DWS数据模板响应DTO
/// DWS data template response DTO
/// </summary>
public record DwsDataTemplateResponseDto
{
    public required string TemplateId { get; init; }
    public required string Name { get; init; }
    public required string Template { get; init; }
    public string Delimiter { get; init; } = ",";
    public bool IsJsonFormat { get; init; }
    public required bool IsEnabled { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
