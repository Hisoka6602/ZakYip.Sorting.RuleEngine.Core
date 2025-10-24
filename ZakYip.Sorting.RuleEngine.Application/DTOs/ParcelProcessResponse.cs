namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理响应DTO
/// </summary>
public class ParcelProcessResponse
{
    /// <summary>
    /// 处理是否成功
    /// Whether processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 包裹ID
    /// Parcel identifier
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 格口号
    /// Chute number
    /// </summary>
    public string? ChuteNumber { get; set; }

    /// <summary>
    /// 错误消息
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 处理时间（毫秒）
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}
