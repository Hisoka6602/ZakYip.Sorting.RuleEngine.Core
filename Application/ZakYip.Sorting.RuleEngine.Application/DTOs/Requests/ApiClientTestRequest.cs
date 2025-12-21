using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// ApiClient测试请求
/// ApiClient Test Request
/// </summary>
public record ApiClientTestRequest
{
    /// <summary>
    /// 包裹条码
    /// Parcel barcode
    /// </summary>
    public required string Barcode { get; init; }

    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// </summary>
    public decimal Weight { get; init; }

    /// <summary>
    /// 长度（厘米）
    /// Length in centimeters
    /// </summary>
    public decimal? Length { get; init; }

    /// <summary>
    /// 宽度（厘米）
    /// Width in centimeters
    /// </summary>
    public decimal? Width { get; init; }

    /// <summary>
    /// 高度（厘米）
    /// Height in centimeters
    /// </summary>
    public decimal? Height { get; init; }

    /// <summary>
    /// 包裹ID（用于NotifyChuteLanding方法）
    /// Parcel ID (for NotifyChuteLanding method)
    /// </summary>
    public string? ParcelId { get; init; }

    /// <summary>
    /// 格口ID（用于NotifyChuteLanding方法）
    /// Chute ID (for NotifyChuteLanding method)
    /// </summary>
    public string? ChuteId { get; init; }
}
