using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// 第三方API配置响应数据传输对象
/// </summary>
public class ThirdPartyApiConfigResponseDto
{
    /// <summary>
    /// 配置ID
    /// </summary>
    /// <example>API001</example>
    [Description("第三方API配置ID")]
    public string ConfigId { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    /// <example>主API服务器</example>
    [Description("第三方API配置名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API基础地址
    /// </summary>
    /// <example>https://api.example.com</example>
    [Description("第三方API基础地址")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API密钥（已脱敏）
    /// </summary>
    /// <example>***********</example>
    [Description("API密钥（已脱敏处理）")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    /// <example>30</example>
    [Description("API请求超时时间（秒）")]
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    /// <example>1</example>
    [Description("API调用优先级（数字越小优先级越高）")]
    public int Priority { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <example>true</example>
    [Description("是否启用此API配置")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    /// <example>POST</example>
    [Description("HTTP请求方法：GET、POST、PUT、DELETE")]
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// 自定义请求头（JSON格式）
    /// </summary>
    /// <example>{"Content-Type":"application/json","Accept":"application/json"}</example>
    [Description("自定义HTTP请求头（JSON格式）")]
    public string? CustomHeaders { get; set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    /// <example>2025-11-04T06:00:00Z</example>
    [Description("配置创建时间（UTC时间）")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间（UTC）
    /// </summary>
    /// <example>2025-11-04T08:30:00Z</example>
    [Description("配置最后更新时间（UTC时间）")]
    public DateTime? UpdatedAt { get; set; }
}
