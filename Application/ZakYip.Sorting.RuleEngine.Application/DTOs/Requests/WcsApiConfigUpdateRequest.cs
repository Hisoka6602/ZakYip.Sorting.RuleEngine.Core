namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// WCS API配置更新请求DTO
/// WCS API configuration update request DTO
/// </summary>
public record WcsApiConfigUpdateRequest
{
    public required string ApiName { get; init; }
    public required string BaseUrl { get; init; }
    public required int TimeoutSeconds { get; init; }
    public string? ApiKey { get; init; }
    public string? CustomHeaders { get; init; }
    public required string HttpMethod { get; init; }
    public string? RequestBodyTemplate { get; init; }
    public required bool IsEnabled { get; init; }
    public required int Priority { get; init; }
    public string? Description { get; init; }
}
