namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 甘特图数据项
/// 用于可视化包裹处理时间线
/// </summary>
public class GanttChartDataItem
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 匹配的规则ID
    /// </summary>
    public string? MatchedRuleId { get; set; }

    /// <summary>
    /// 格口ID
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 格口编号
    /// </summary>
    public string? ChuteCode { get; set; }

    /// <summary>
    /// 格口名称
    /// </summary>
    public string? ChuteName { get; set; }

    /// <summary>
    /// 匹配时间
    /// </summary>
    public DateTime MatchingTime { get; set; }

    /// <summary>
    /// 是否匹配成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// DWS通信时间
    /// </summary>
    public DateTime? DwsCommunicationTime { get; set; }

    /// <summary>
    /// API通信请求时间
    /// </summary>
    public DateTime? ApiRequestTime { get; set; }

    /// <summary>
    /// API通信耗时（毫秒）
    /// </summary>
    public long? ApiDurationMs { get; set; }

    /// <summary>
    /// 重量（克）
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// 体积（立方厘米）
    /// </summary>
    public decimal? Volume { get; set; }

    /// <summary>
    /// 小车占位数量
    /// </summary>
    public int CartOccupancy { get; set; }

    /// <summary>
    /// 在查询结果中的顺序
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// 是否是查询的目标包裹
    /// </summary>
    public bool IsTarget { get; set; }
}
