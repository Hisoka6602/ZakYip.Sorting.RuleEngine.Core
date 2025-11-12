namespace ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

/// <summary>
/// 图片信息值对象
/// Image information value object
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// 设备名称
    /// Device name
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 图片本地路径
    /// Local path of the image
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// 图片拍摄时间（可选）
    /// Image capture timestamp (optional)
    /// </summary>
    public DateTime? CapturedAt { get; set; }

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    public ImageInfo()
    {
    }

    /// <summary>
    /// 构造函数
    /// Constructor with parameters
    /// </summary>
    /// <param name="deviceName">设备名称 / Device name</param>
    /// <param name="localPath">本地路径 / Local path</param>
    public ImageInfo(string deviceName, string localPath)
    {
        DeviceName = deviceName;
        LocalPath = localPath;
    }

    /// <summary>
    /// 构造函数
    /// Constructor with parameters including capture time
    /// </summary>
    /// <param name="deviceName">设备名称 / Device name</param>
    /// <param name="localPath">本地路径 / Local path</param>
    /// <param name="capturedAt">拍摄时间 / Captured timestamp</param>
    public ImageInfo(string deviceName, string localPath, DateTime capturedAt)
    {
        DeviceName = deviceName;
        LocalPath = localPath;
        CapturedAt = capturedAt;
    }
}
