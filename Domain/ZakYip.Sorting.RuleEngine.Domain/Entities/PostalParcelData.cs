namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 邮政包裹数据实体
/// Postal parcel data entity
/// </summary>
public class PostalParcelData
{
    /// <summary>
    /// 条码/运单号
    /// Barcode/Tracking number
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
    public decimal? Length { get; set; }

    /// <summary>
    /// 宽度（单位：毫米）
    /// Width in millimeters
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// 高度（单位：毫米）
    /// Height in millimeters
    /// </summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// 体积（单位：立方厘米）
    /// Volume in cubic centimeters
    /// </summary>
    public decimal? Volume { get; set; }

    /// <summary>
    /// 寄件人地址
    /// Sender address
    /// </summary>
    public string? SenderAddress { get; set; }

    /// <summary>
    /// 收件人地址
    /// Recipient address
    /// </summary>
    public string? RecipientAddress { get; set; }

    /// <summary>
    /// 目的地代码
    /// Destination code
    /// </summary>
    public string? DestinationCode { get; set; }

    /// <summary>
    /// 扫描时间
    /// Scan timestamp
    /// </summary>
    public DateTime ScannedAt { get; set; } = DateTime.Now;
}
