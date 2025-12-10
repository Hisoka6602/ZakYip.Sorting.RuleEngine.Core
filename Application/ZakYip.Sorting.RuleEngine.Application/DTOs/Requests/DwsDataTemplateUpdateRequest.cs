namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// DWS数据模板更新请求DTO
/// DWS data template update request DTO
/// </summary>
public record DwsDataTemplateUpdateRequest
{
    public required string Name { get; init; }
    public required string Template { get; init; }
    public string Delimiter { get; init; } = ",";
    public bool IsJsonFormat { get; init; }
    public required bool IsEnabled { get; init; }
    public string? Description { get; init; }
}
