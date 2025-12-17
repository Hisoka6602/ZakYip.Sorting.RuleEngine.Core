namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 邮政处理中心API完整配置
/// Postal Processing Center API Full Configuration Settings
/// </summary>
public class PostProcessingCenterFullApiSettings
{
    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = "http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl";

    /// <summary>
    /// 超时时间 (Timeout in milliseconds)
    /// </summary>
    public int Timeout { get; set; } = 1000;

    /// <summary>
    /// 车间代码 (Workshop code)
    /// </summary>
    public string WorkshopCode { get; set; } = "WS43400001";

    /// <summary>
    /// 设备ID (Device ID)
    /// </summary>
    public string DeviceId { get; set; } = "43400002";

    /// <summary>
    /// 员工号 (Employee number)
    /// </summary>
    public string EmployeeNumber { get; set; } = "03178298";

    /// <summary>
    /// 本地服务Url
    /// </summary>
    public string LocalServiceUrl { get; set; } = string.Empty;
}
