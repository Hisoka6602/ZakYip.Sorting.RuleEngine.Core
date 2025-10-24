namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS通信日志实体
/// </summary>
public class DwsCommunicationLog
{
    /// <summary>
    /// 日志ID（自增主键）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// DWS地址
    /// </summary>
    public string DwsAddress { get; set; } = string.Empty;

    /// <summary>
    /// 接收的原始内容
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// 格式化内容（JSON格式）
    /// </summary>
    public string? FormattedContent { get; set; }

    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 重量（克）
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// 体积（立方厘米）
    /// </summary>
    public decimal? Volume { get; set; }

    /// <summary>
    /// 通信时间
    /// </summary>
    public DateTime CommunicationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
