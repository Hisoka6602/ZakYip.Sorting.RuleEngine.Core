using ZakYip.Sorting.RuleEngine.Domain.ValueObjects;
using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 重量（单位：克）
    /// Weight in grams
    public decimal Weight { get; set; }
    /// 长度（单位：毫米）
    /// Length in millimeters
    public decimal Length { get; set; }
    /// 宽度（单位：毫米）
    /// Width in millimeters
    public decimal Width { get; set; }
    /// 高度（单位：毫米）
    /// Height in millimeters
    public decimal Height { get; set; }
    /// 体积（单位：立方厘米）
    /// Volume in cubic centimeters
    public decimal Volume { get; set; }
    /// 扫描时间
    /// Scan timestamp
    public DateTime ScannedAt { get; set; } = SystemClockProvider.LocalNow;
    /// 图片信息集合（一个包裹可对应N个图片）
    /// Collection of images (a parcel can have multiple images)
    public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();
}
