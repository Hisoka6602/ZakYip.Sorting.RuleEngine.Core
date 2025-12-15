using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 包裹ID
    public string ParcelId { get; set; } = string.Empty;
    /// 关联的DWS内容（JSON格式）
    public string? DwsContent { get; set; }
    /// 关联的API内容（JSON格式）
    public string? ApiContent { get; set; }
    /// 匹配的规则ID
    public string? MatchedRuleId { get; set; }
    /// 匹配依据
    public string? MatchingReason { get; set; }
    /// 格口ID
    public long? ChuteId { get; set; }
    /// 小车占位数量
    public int CartOccupancy { get; set; }
    /// 匹配时间
    public DateTime MatchingTime { get; set; } = SystemClockProvider.LocalNow;
    /// 是否成功
    public bool IsSuccess { get; set; }
    /// 错误信息
    public string? ErrorMessage { get; set; }
}
