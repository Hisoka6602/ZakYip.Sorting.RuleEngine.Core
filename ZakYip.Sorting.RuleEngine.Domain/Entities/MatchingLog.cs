namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 匹配日志实体
/// </summary>
public class MatchingLog
{
    /// <summary>
    /// 日志ID（自增主键）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 关联的DWS内容（JSON格式）
    /// </summary>
    public string? DwsContent { get; set; }

    /// <summary>
    /// 关联的API内容（JSON格式）
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
    public DateTime MatchingTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
