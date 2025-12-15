using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 通信类型（TCP/SignalR/HTTP/MQTT）
    public CommunicationType CommunicationType { get; set; }
    /// DWS地址
    public string DwsAddress { get; set; } = string.Empty;
    /// 接收的原始内容
    public string OriginalContent { get; set; } = string.Empty;
    /// 格式化内容（JSON格式）
    public string? FormattedContent { get; set; }
    /// 条码
    public string? Barcode { get; set; }
    /// 重量（克）
    public decimal? Weight { get; set; }
    /// 体积（立方厘米）
    public decimal? Volume { get; set; }
    /// 图片信息（JSON格式存储）
    /// Images information stored in JSON format
    public string? ImagesJson { get; set; }
    /// 通信时间
    public DateTime CommunicationTime { get; set; } = SystemClockProvider.LocalNow;
    /// 是否成功
    public bool IsSuccess { get; set; }
    /// 错误信息
    public string? ErrorMessage { get; set; }
}
