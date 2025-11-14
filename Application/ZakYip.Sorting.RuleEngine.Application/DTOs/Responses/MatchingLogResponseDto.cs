namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 匹配日志响应DTO
/// </summary>
public record MatchingLogResponseDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; init; }

    /// <summary>
    /// DWS内容（JSON格式）
    /// </summary>
    public string? DwsContent { get; init; }

    /// <summary>
    /// API内容（JSON格式）
    /// </summary>
    public string? ApiContent { get; init; }

    /// <summary>
    /// 匹配的规则ID
    /// </summary>
    public string? MatchedRuleId { get; init; }

    /// <summary>
    /// 匹配依据
    /// </summary>
    public string? MatchingReason { get; init; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public long? ChuteId { get; init; }

    /// <summary>
    /// 小车占位数量
    /// </summary>
    public int CartOccupancy { get; init; }

    /// <summary>
    /// 匹配时间
    /// </summary>
    public DateTime MatchingTime { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }
}
