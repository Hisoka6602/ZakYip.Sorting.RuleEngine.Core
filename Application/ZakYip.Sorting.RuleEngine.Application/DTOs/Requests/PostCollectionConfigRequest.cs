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
    public required string BaseUrl { get; init; }
    
    /// <summary>
    /// 超时时间（秒）
    /// Timeout (seconds)
    /// </summary>
    /// <example>30</example>
    public int TimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    /// <example>false</example>
    public bool Enabled { get; init; } = false;
}
