namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 邮政处理中心API配置请求
/// Postal Processing Center API Configuration Request
/// 参数结构与 ApiParameters 保持一致
/// Parameter structure is consistent with ApiParameters
/// </summary>
public record PostProcessingCenterConfigRequest
{
    /// <summary>
    /// URL
    /// </summary>
    /// <example>http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl</example>
    public required string Url { get; init; }

    /// <summary>
    /// 超时时间 (Timeout in milliseconds)
    /// </summary>
    /// <example>1000</example>
    public int Timeout { get; init; } = 1000;

    /// <summary>
    /// 车间代码 (Workshop code)
    /// </summary>
    /// <example>WS43400001</example>
    public required string WorkshopCode { get; init; }

    /// <summary>
    /// 设备ID (Device ID)
    /// </summary>
    /// <example>43400002</example>
    public required string DeviceId { get; init; }

    /// <summary>
    /// 员工号 (Employee number)
    /// </summary>
    /// <example>03178298</example>
    public required string EmployeeNumber { get; init; }

    /// <summary>
    /// 本地服务Url
    /// Local service URL
    /// </summary>
    /// <example></example>
    public string LocalServiceUrl { get; init; } = string.Empty;
    
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
