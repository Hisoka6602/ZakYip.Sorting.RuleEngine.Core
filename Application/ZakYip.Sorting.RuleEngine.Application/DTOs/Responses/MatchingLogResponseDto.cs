namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 匹配日志响应DTO
/// </summary>
public class MatchingLogResponseDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// DWS内容（JSON格式）
    /// </summary>
    public string? DwsContent { get; set; }

    /// <summary>
    /// API内容（JSON格式）
    /// </summary>
    public string? ApiContent { get; set; }

    /// <summary>
    /// 匹配的规则ID
    /// </summary>
    public string? MatchedRuleId { get; set; }

    /// <summary>
    /// 匹配依据
    /// </summary>
    public string? MatchingReason { get; set; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 小车占位数量
    /// </summary>
    public int CartOccupancy { get; set; }

    /// <summary>
    /// 匹配时间
    /// </summary>
    public DateTime MatchingTime { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
