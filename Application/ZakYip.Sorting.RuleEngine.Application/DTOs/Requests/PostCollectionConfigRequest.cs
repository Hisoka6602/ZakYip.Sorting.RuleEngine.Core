namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 邮政分揽投机构API配置请求
/// Postal Collection Institution API Configuration Request
/// </summary>
public record PostCollectionConfigRequest
{
    /// <summary>
    /// API基础URL (SOAP端点，不含?wsdl)
    /// API Base URL (SOAP endpoint, without ?wsdl)
    /// </summary>
    /// <example>http://10.4.201.115/pcs-ci-job/WyService/services/CommWY</example>
    public required string Url { get; init; }
    
    /// <summary>
    /// 车间代码
    /// Workshop code
    /// </summary>
    public required string WorkshopCode { get; init; }
    
    /// <summary>
    /// 设备ID
    /// Device ID
    /// </summary>
    public required string DeviceId { get; init; }
    
    /// <summary>
    /// 公司名称
    /// Company name
    /// </summary>
    public required string CompanyName { get; init; }
    
    /// <summary>
    /// 设备条码
    /// Device barcode
    /// </summary>
    public required string DeviceBarcode { get; init; }
    
    /// <summary>
    /// 机构编号
    /// Organization number
    /// </summary>
    public required string OrganizationNumber { get; init; }
    
    /// <summary>
    /// 员工编号
    /// Employee number
    /// </summary>
    public required string EmployeeNumber { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;
    
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
