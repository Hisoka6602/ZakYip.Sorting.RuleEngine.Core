using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理请求DTO
/// </summary>
[SwaggerSchema(Description = "包裹处理请求数据传输对象，包含包裹的基本信息和DWS数据")]
public record class ParcelProcessRequest
{
    /// <summary>
    /// 包裹ID
    /// 示例: PKG20231101001
    /// </summary>
    [SwaggerSchema(Description = "包裹唯一标识")]
    public required string ParcelId { get; init; }

    /// <summary>
    /// 小车号
    /// 示例: CART001
    /// </summary>
    [SwaggerSchema(Description = "小车编号")]
    public required string CartNumber { get; init; }

    /// <summary>
    /// 条码
    /// 示例: 1234567890123
    /// </summary>
    [SwaggerSchema(Description = "包裹条码")]
    public string? Barcode { get; init; }

    /// <summary>
    /// 重量（克）
    /// 示例: 2500.5
    /// </summary>
    [Range(0, 999999999, ErrorMessage = "重量必须大于等于0")]
    [SwaggerSchema(Description = "包裹重量(克)")]
    public decimal? Weight { get; init; }

    /// <summary>
    /// 长度（毫米）
    /// 示例: 300
    /// </summary>
    [Range(0, 999999999, ErrorMessage = "长度必须大于等于0")]
    [SwaggerSchema(Description = "包裹长度(毫米)")]
    public decimal? Length { get; init; }

    /// <summary>
    /// 宽度（毫米）
    /// 示例: 200
    /// </summary>
    [Range(0, 999999999, ErrorMessage = "宽度必须大于等于0")]
    [SwaggerSchema(Description = "包裹宽度(毫米)")]
    public decimal? Width { get; init; }

    /// <summary>
    /// 高度（毫米）
    /// 示例: 150
    /// </summary>
    [Range(0, 999999999, ErrorMessage = "高度必须大于等于0")]
    [SwaggerSchema(Description = "包裹高度(毫米)")]
    public decimal? Height { get; init; }

    /// <summary>
    /// 体积（立方厘米）
    /// 示例: 9000
    /// </summary>
    [Range(0, 999999999, ErrorMessage = "体积必须大于等于0")]
    [SwaggerSchema(Description = "包裹体积(立方厘米)")]
    public decimal? Volume { get; init; }
}
