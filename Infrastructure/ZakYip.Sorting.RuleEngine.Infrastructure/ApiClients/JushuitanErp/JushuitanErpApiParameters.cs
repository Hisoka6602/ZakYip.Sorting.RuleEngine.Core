namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;

/// <summary>
/// 聚水潭ERP API参数配置类
/// Jushuituan ERP API Parameters Configuration
/// </summary>
public class JushuitanErpApiParameters
{
    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; } = "https://openapi.jushuitan.com/open/orders/weight/send/upload";

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeOut { get; set; } = 5000;

    /// <summary>
    /// AppKey
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// AppSecret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// AccessToken
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public int Version { get; set; } = 2;

    /// <summary>
    /// 是否上传重量（默认值 true）
    /// </summary>
    public bool IsUploadWeight { get; set; } = true;

    /// <summary>
    /// 称重类型（默认值为 1）
    /// 0: 验货后称重
    /// 1: 验货后称重并发货
    /// 2: 无须验货称重
    /// 3: 无须验货称重并发货
    /// 4: 发货后称重
    /// 5: 自动判断称重并发货
    /// </summary>
    public int Type { get; set; } = 1;

    /// <summary>
    /// 是否为国际运单号（默认值 false，表示国内快递）
    /// </summary>
    public bool IsUnLid { get; set; } = false;

    /// <summary>
    /// 称重来源备注（会显示在订单操作日志中）
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 默认重量（当无重量数据时使用）
    /// </summary>
    public double DefaultWeight { get; set; } = -1;
}
