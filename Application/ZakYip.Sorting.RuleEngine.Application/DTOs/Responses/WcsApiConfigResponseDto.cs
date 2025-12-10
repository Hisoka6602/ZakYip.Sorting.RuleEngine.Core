using System.ComponentModel;

namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// WCS API配置响应数据传输对象（单例模式，不包含ID）
/// WCS API configuration response DTO (Singleton pattern, no ID)
/// </summary>
public record class WcsApiConfigResponseDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    /// <example>主API服务器</example>
    [Description("WCS API配置名称")]
    public required string Name { get; init; }

    /// <summary>
    /// API基础地址
    /// </summary>
    /// <example>https://api.example.com</example>
    [Description("WCS API基础地址")]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// API密钥（已脱敏）
    /// </summary>
    /// <example>***********</example>
    [Description("API密钥（已脱敏处理）")]
    public string? ApiKey { get; init; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    /// <example>30</example>
    [Description("API请求超时时间（秒）")]
    public required int TimeoutSeconds { get; init; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    /// <example>1</example>
    [Description("API调用优先级（数字越小优先级越高）")]
    public required int Priority { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <example>true</example>
    [Description("是否启用此API配置")]
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    /// <example>POST</example>
    [Description("HTTP请求方法：GET、POST、PUT、DELETE")]
    public required string HttpMethod { get; init; }

    /// <summary>
    /// 自定义请求头（JSON格式）
    /// </summary>
    /// <example>{"Content-Type":"application/json","Accept":"application/json"}</example>
    [Description("自定义HTTP请求头（JSON格式）")]
    public string? CustomHeaders { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    /// <example>2025-11-04T06:00:00</example>
    [Description("配置创建时间")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    /// <example>2025-11-04T08:30:00</example>
    [Description("配置最后更新时间")]
    public DateTime? UpdatedAt { get; init; }
}
