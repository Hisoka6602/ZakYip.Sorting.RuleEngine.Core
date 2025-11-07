namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 邮政API响应实体
/// Postal API response entity
/// </summary>
public class PostalApiResponse
{
    /// <summary>
    /// 响应是否成功
    /// Whether the response is successful
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
    /// 响应时间
    /// Response timestamp
    /// </summary>
    public DateTime ResponseTime { get; set; } = DateTime.Now;
}
