namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// ApiClient测试请求
/// ApiClient Test Request
/// </summary>
public class ApiClientTestRequest
{
    /// <summary>
    /// 包裹条码
    /// Parcel barcode
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 重量（克）
    /// Weight in grams
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// 长度（厘米）
    /// Length in centimeters
    /// </summary>
    public decimal? Length { get; set; }

    /// <summary>
    /// 宽度（厘米）
    /// Width in centimeters
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// 高度（厘米）
    /// Height in centimeters
    /// </summary>
    public decimal? Height { get; set; }
}
