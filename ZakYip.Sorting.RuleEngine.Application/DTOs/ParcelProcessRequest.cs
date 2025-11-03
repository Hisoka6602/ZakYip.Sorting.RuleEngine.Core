using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理请求DTO
/// </summary>
[SwaggerSchema(Description = "包裹处理请求数据传输对象，包含包裹的基本信息和DWS数据")]
public class ParcelProcessRequest
{
    /// <summary>
    /// 包裹ID
    /// Parcel identifier
    /// Example: PKG20231101001
    /// </summary>
    [Required(ErrorMessage = "包裹ID不能为空")]
    [SwaggerSchema("包裹唯一标识")]
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 小车号
    /// Cart number
    /// Example: CART001
    /// </summary>
    [Required(ErrorMessage = "小车号不能为空")]
    [SwaggerSchema("小车编号")]
    public string CartNumber { get; set; } = string.Empty;

    /// <summary>
    /// 条码
    /// Barcode
    /// Example: 1234567890123
    /// </summary>
    [SwaggerSchema("包裹条码")]
    public string? Barcode { get; set; }

    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// Example: 2500.5
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "重量必须大于0")]
    [SwaggerSchema("包裹重量(克)")]
    public decimal? Weight { get; set; }

    /// <summary>
    /// 长度（毫米）
    /// Length in millimeters
    /// Example: 300
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "长度必须大于0")]
    [SwaggerSchema("包裹长度(毫米)")]
    public decimal? Length { get; set; }

    /// <summary>
    /// 宽度（毫米）
    /// Width in millimeters
    /// Example: 200
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "宽度必须大于0")]
    [SwaggerSchema("包裹宽度(毫米)")]
    public decimal? Width { get; set; }

    /// <summary>
    /// 高度（毫米）
    /// Height in millimeters
    /// Example: 150
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "高度必须大于0")]
    [SwaggerSchema("包裹高度(毫米)")]
    public decimal? Height { get; set; }

    /// <summary>
    /// 体积（立方厘米）
    /// Volume in cubic centimeters
    /// Example: 9000
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "体积必须大于0")]
    [SwaggerSchema("包裹体积(立方厘米)")]
    public decimal? Volume { get; set; }
}
