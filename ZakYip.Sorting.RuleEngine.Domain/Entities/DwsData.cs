namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// DWS（尺寸重量扫描）数据实体
/// </summary>
public class DwsData
{
    /// <summary>
    /// 条码
    /// Barcode identifier
    /// </summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 重量（单位：克）
    /// Weight in grams
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// 长度（单位：毫米）
    /// Length in millimeters
    /// </summary>
    public decimal Length { get; set; }

    /// <summary>
    /// 宽度（单位：毫米）
    /// Width in millimeters
    /// </summary>
    public decimal Width { get; set; }

    /// <summary>
    /// 高度（单位：毫米）
    /// Height in millimeters
    /// </summary>
    public decimal Height { get; set; }

    /// <summary>
    /// 体积（单位：立方厘米）
    /// Volume in cubic centimeters
    /// </summary>
    public decimal Volume { get; set; }

    /// <summary>
    /// 扫描时间
    /// Scan timestamp
    /// </summary>
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
