using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理响应DTO
/// </summary>
[SwaggerSchema(Description = "包裹处理响应数据传输对象，包含处理结果和分配的格口信息")]
public class ParcelProcessResponse
{
    /// <summary>
    /// 处理是否成功
    /// Whether processing was successful
    /// Example: true
    /// </summary>
    [SwaggerSchema("处理是否成功")]
    public bool Success { get; set; }

    /// <summary>
    /// 包裹ID
    /// Parcel identifier
    /// Example: PKG20231101001
    /// </summary>
    [SwaggerSchema("包裹唯一标识")]
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 格口号
    /// Chute number
    /// Example: CHUTE01
    /// </summary>
    [SwaggerSchema("分配的格口号")]
    public string? ChuteNumber { get; set; }

    /// <summary>
    /// 错误消息
    /// Error message if processing failed
    /// Example: null
    /// </summary>
    [SwaggerSchema("错误消息(处理失败时)")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 处理时间（毫秒）
    /// Processing time in milliseconds
    /// Example: 15
    /// </summary>
    [SwaggerSchema("处理耗时(毫秒)")]
    public long ProcessingTimeMs { get; set; }
}
