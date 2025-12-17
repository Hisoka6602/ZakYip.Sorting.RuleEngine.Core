namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 邮政处理中心API完整配置请求
/// Postal Processing Center API Full Configuration Request
/// </summary>
public record PostProcessingCenterFullConfigRequest
{
    /// <summary>
    /// URL
    /// </summary>
    /// <example>http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl</example>
    public required string Url { get; set; }

    /// <summary>
    /// 超时时间 (Timeout in milliseconds)
    /// </summary>
    /// <example>1000</example>
    public int Timeout { get; set; } = 1000;

    /// <summary>
    /// 车间代码 (Workshop code)
    /// </summary>
    /// <example>WS43400001</example>
    public required string WorkshopCode { get; set; }

    /// <summary>
    /// 设备ID (Device ID)
    /// </summary>
    /// <example>43400002</example>
    public required string DeviceId { get; set; }

    /// <summary>
    /// 员工号 (Employee number)
    /// </summary>
    /// <example>03178298</example>
    public required string EmployeeNumber { get; set; }

    /// <summary>
    /// 本地服务Url
    /// </summary>
    /// <example></example>
    public string LocalServiceUrl { get; set; } = string.Empty;
}
