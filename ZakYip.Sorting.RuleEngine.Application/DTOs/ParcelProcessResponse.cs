using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理响应DTO
/// </summary>
[SwaggerSchema(Description = "包裹处理响应数据传输对象，包含处理结果和分配的格口信息")]
public record class ParcelProcessResponse
{
    /// <summary>
    /// 处理是否成功
    /// Whether processing was successful
    /// Example: true
    /// </summary>
    [SwaggerSchema(Description = "处理是否成功")]
    public required bool Success { get; init; }

    /// <summary>
    /// 包裹ID
    /// Parcel identifier
    /// Example: PKG20231101001
    /// </summary>
    [SwaggerSchema(Description = "包裹唯一标识")]
    public required string ParcelId { get; init; }

    /// <summary>
    /// 格口号
    /// Chute number
    /// Example: CHUTE01
    /// </summary>
    [SwaggerSchema(Description = "分配的格口号")]
    public string? ChuteNumber { get; init; }

    /// <summary>
    /// 错误消息
    /// Error message if processing failed
    /// Example: null
    /// </summary>
    [SwaggerSchema(Description = "错误消息(处理失败时)")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 处理时间（毫秒）
    /// Processing time in milliseconds
    /// Example: 15
    /// </summary>
    [SwaggerSchema(Description = "处理耗时(毫秒)")]
    public required long ProcessingTimeMs { get; init; }
}
