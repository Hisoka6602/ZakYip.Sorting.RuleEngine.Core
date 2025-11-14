namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 通信日志响应DTO
/// </summary>
public record CommunicationLogResponseDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string? ParcelId { get; init; }

    /// <summary>
    /// 远程地址
    /// </summary>
    public string? RemoteAddress { get; init; }

    /// <summary>
    /// 通信类型（发送/接收）
    /// </summary>
    public string? CommunicationType { get; init; }

    /// <summary>
    /// 原始内容
    /// </summary>
    public string? RawContent { get; init; }

    /// <summary>
    /// 格式化内容
    /// </summary>
    public string? FormattedContent { get; init; }

    /// <summary>
    /// 通信时间
    /// </summary>
    public DateTime CommunicationTime { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; init; }
}
