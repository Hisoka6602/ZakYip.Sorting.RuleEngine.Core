namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 邮政分揽投机构API完整配置请求
/// Postal Collection Institution API Full Configuration Request
/// </summary>
public record PostCollectionFullConfigRequest
{
    /// <summary>
    /// URL
    /// </summary>
    /// <example>http://10.4.201.115/pcs-ci-job/WyService/services/CommWY?wsdl</example>
    public required string Url { get; init; }

    /// <summary>
    /// 超时时间 (Timeout in milliseconds)
    /// </summary>
    /// <example>1000</example>
    public int Timeout { get; init; } = 1000;

    /// <summary>
    /// 车间代码 (Workshop code)
    /// </summary>
    /// <example>WS20140010</example>
    public required string WorkshopCode { get; init; }

    /// <summary>
    /// 设备ID (Device ID)
    /// </summary>
    /// <example>20140010</example>
    public required string DeviceId { get; init; }

    /// <summary>
    /// 公司名称 (Company name)
    /// </summary>
    /// <example>广东泽业科技有限公司</example>
    public required string CompanyName { get; init; }

    /// <summary>
    /// 设备条码 (Device barcode)
    /// </summary>
    /// <example>141562320001131</example>
    public required string DeviceBarcode { get; init; }

    /// <summary>
    /// 机构号 (Organization number)
    /// </summary>
    /// <example>20140011</example>
    public required string OrganizationNumber { get; init; }

    /// <summary>
    /// 员工号 (Employee number)
    /// </summary>
    /// <example>00818684</example>
    public required string EmployeeNumber { get; init; }

    /// <summary>
    /// 是否使用顶扫稽核
    /// </summary>
    /// <example>false</example>
    public bool IsUseCsb { get; init; }

    /// <summary>
    /// 稽核参数
    /// </summary>
    public CsbConfigParameters? CsbInfo { get; init; }
}

/// <summary>
/// 顶扫稽核配置参数
/// CSB (Top Scan Audit) Configuration Parameters
/// </summary>
public record CsbConfigParameters
{
    /// <summary>
    /// 稽核服务URL
    /// </summary>
    public string? AuditServiceUrl { get; init; }

    /// <summary>
    /// 稽核超时时间（毫秒）
    /// </summary>
    public int? AuditTimeout { get; init; }
}
