namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 第三方API响应实体
/// </summary>
public class ThirdPartyResponse
{
    /// <summary>
    /// 响应是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据（JSON格式）
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// OCR识别数据（如果有）
    /// </summary>
    public OcrData? OcrData { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime ResponseTime { get; set; } = DateTime.Now;
}
