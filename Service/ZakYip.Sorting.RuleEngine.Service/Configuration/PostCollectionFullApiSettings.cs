namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 邮政分揽投机构API完整配置
/// Postal Collection Institution API Full Configuration Settings
/// </summary>
public class PostCollectionFullApiSettings
{
    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = "http://10.4.201.115/pcs-ci-job/WyService/services/CommWY?wsdl";

    /// <summary>
    /// 超时时间 (Timeout in milliseconds)
    /// </summary>
    public int Timeout { get; set; } = 1000;

    /// <summary>
    /// 车间代码 (Workshop code)
    /// </summary>
    public string WorkshopCode { get; set; } = "WS20140010";

    /// <summary>
    /// 设备ID (Device ID)
    /// </summary>
    public string DeviceId { get; set; } = "20140010";

    /// <summary>
    /// 公司名称 (Company name)
    /// </summary>
    public string CompanyName { get; set; } = "广东泽业科技有限公司";

    /// <summary>
    /// 设备条码 (Device barcode)
    /// </summary>
    public string DeviceBarcode { get; set; } = "141562320001131";

    /// <summary>
    /// 机构号 (Organization number)
    /// </summary>
    public string OrganizationNumber { get; set; } = "20140011";

    /// <summary>
    /// 员工号 (Employee number)
    /// </summary>
    public string EmployeeNumber { get; set; } = "00818684";

    /// <summary>
    /// 是否使用顶扫稽核
    /// </summary>
    public bool IsUseCsb { get; set; }

    /// <summary>
    /// 稽核参数
    /// </summary>
    public CsbSettings CsbInfo { get; set; } = new();
}

/// <summary>
/// 顶扫稽核配置
/// CSB (Top Scan Audit) Configuration
/// </summary>
public class CsbSettings
{
    /// <summary>
    /// 稽核服务URL
    /// </summary>
    public string AuditServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// 稽核超时时间（毫秒）
    /// </summary>
    public int AuditTimeout { get; set; } = 1000;
}
