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
    /// 响应代码
    /// Response code
    public string Code { get; set; } = string.Empty;
    /// 响应消息
    /// Response message
    public string Message { get; set; } = string.Empty;
    /// 响应数据（JSON格式）
    /// Response data (JSON format)
    public string? Data { get; set; }
    /// 错误信息
    /// Error message
    public string? ErrorMessage { get; set; }
    /// OCR识别数据（如果有）
    public OcrData? OcrData { get; set; }
}
