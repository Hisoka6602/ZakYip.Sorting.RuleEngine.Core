namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API响应实体
/// WCS API response entity
/// 字段与ApiCommunicationLog一致，以便持久化
/// Fields are consistent with ApiCommunicationLog for persistence
/// </summary>
public class WcsApiResponse : BaseApiCommunication
{
    /// <summary>
    /// 响应是否成功
    /// Is response successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应代码
    /// Response code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据（JSON格式）
    /// Response data (JSON format)
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 错误信息
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// OCR识别数据（如果有）
    /// </summary>
    public OcrData? OcrData { get; set; }
}
