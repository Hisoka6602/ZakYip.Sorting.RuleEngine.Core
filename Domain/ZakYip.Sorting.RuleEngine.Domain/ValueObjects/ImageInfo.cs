namespace ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

/// <summary>
/// 图片信息值对象
/// Image information value object
/// </summary>
public record ImageInfo
{
    /// <summary>
    /// 设备名称
    /// Device name
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// 图片本地路径
    /// Local path of the image
    /// </summary>
    public required string LocalPath { get; init; }

    /// <summary>
    /// 图片拍摄时间（可选）
    /// Image capture timestamp (optional)
    /// </summary>
    public DateTime? CapturedAt { get; init; }
}
