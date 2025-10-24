namespace ZakYip.Sorting.RuleEngine.Application.DTOs;

/// <summary>
/// 包裹处理请求DTO
/// </summary>
public class ParcelProcessRequest
{
    /// <summary>
    /// 包裹ID
    /// Parcel identifier
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 小车号
    /// Cart number
    /// </summary>
    public string CartNumber { get; set; } = string.Empty;

    /// <summary>
    /// 条码
    /// Barcode
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// 长度（毫米）
    /// Length in millimeters
    /// </summary>
    public decimal? Length { get; set; }

    /// <summary>
    /// 宽度（毫米）
    /// Width in millimeters
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// 高度（毫米）
    /// Height in millimeters
    /// </summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// 体积（立方厘米）
    /// Volume in cubic centimeters
    /// </summary>
    public decimal? Volume { get; set; }
}
