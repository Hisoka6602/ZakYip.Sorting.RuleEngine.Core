namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 通信日志响应DTO
/// </summary>
public class CommunicationLogResponseDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// 远程地址
    /// </summary>
    public string? RemoteAddress { get; set; }

    /// <summary>
    /// 通信类型（发送/接收）
    /// </summary>
    public string? CommunicationType { get; set; }

    /// <summary>
    /// 原始内容
    /// </summary>
    public string? RawContent { get; set; }

    /// <summary>
    /// 格式化内容
    /// </summary>
    public string? FormattedContent { get; set; }

    /// <summary>
    /// 通信时间
    /// </summary>
    public DateTime CommunicationTime { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
