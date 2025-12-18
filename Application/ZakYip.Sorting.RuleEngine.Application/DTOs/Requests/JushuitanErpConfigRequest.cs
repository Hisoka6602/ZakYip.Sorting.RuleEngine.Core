namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 聚水潭ERP API配置请求
/// Jushuituan ERP API Configuration Request
/// </summary>
public record JushuitanErpConfigRequest
{
    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public string Name { get; init; } = "聚水潭ERP配置";
    
    /// <summary>
    /// Url
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>
    /// AppKey
    /// </summary>
    public required string AppKey { get; init; }

    /// <summary>
    /// AppSecret
    /// </summary>
    public required string AppSecret { get; init; }

    /// <summary>
    /// AccessToken
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// 版本
    /// </summary>
    public int Version { get; init; } = 2;

    /// <summary>
    /// 是否上传重量（默认值 true）
    /// </summary>
    public bool IsUploadWeight { get; init; } = true;

    /// <summary>
    /// 称重类型（默认值为 1）
    /// 0: 验货后称重
    /// 1: 验货后称重并发货
    /// 2: 无须验货称重
    /// 3: 无须验货称重并发货
    /// 4: 发货后称重
    /// 5: 自动判断称重并发货
    /// </summary>
    public int Type { get; init; } = 1;

    /// <summary>
    /// 是否为国际运单号（默认值 false，表示国内快递）
    /// </summary>
    public bool IsUnLid { get; init; } = false;

    /// <summary>
    /// 称重来源备注（会显示在订单操作日志中）
    /// </summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// 默认重量（当无重量数据时使用）
    /// </summary>
    public double DefaultWeight { get; init; } = -1;
    
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; init; }
}
