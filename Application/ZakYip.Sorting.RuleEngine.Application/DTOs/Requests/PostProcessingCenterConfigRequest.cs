namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 邮政处理中心API配置请求
/// Postal Processing Center API Configuration Request
/// </summary>
public record PostProcessingCenterConfigRequest
{
    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// API基础URL (SOAP端点，不含?wsdl)
    /// API Base URL (SOAP endpoint, without ?wsdl)
    /// </summary>
    /// <example>http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY</example>
    public required string Url { get; set; }
    
    /// <summary>
    /// 车间代码
    /// Workshop code
    /// </summary>
    public required string WorkshopCode { get; set; }
    
    /// <summary>
    /// 设备ID
    /// Device ID
    /// </summary>
    public required string DeviceId { get; set; }
    
    /// <summary>
    /// 公司名称
    /// Company name
    /// </summary>
    public required string CompanyName { get; set; }
    
    /// <summary>
    /// 设备条码
    /// Device barcode
    /// </summary>
    public required string DeviceBarcode { get; set; }
    
    /// <summary>
    /// 机构编号
    /// Organization number
    /// </summary>
    public required string OrganizationNumber { get; set; }
    
    /// <summary>
    /// 员工编号
    /// Employee number
    /// </summary>
    public required string EmployeeNumber { get; set; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; set; }
}
