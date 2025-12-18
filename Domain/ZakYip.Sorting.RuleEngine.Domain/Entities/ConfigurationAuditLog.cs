namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 配置审计日志实体 / Configuration Audit Log Entity
/// </summary>
/// <remarks>
/// 记录所有配置变更的审计信息，包括变更时间、变更前后的内容。
/// Records all configuration change audit information, including change time, before and after content.
/// </remarks>
public record class ConfigurationAuditLog
{
    /// <summary>
    /// 审计日志ID（主键）/ Audit Log ID (Primary Key)
    /// </summary>
    public long AuditId { get; init; }
    
    /// <summary>
    /// 配置类型 / Configuration Type
    /// </summary>
    /// <remarks>
    /// 例如: DwsConfig, SorterConfig, DwsTimeoutConfig 等
    /// Examples: DwsConfig, SorterConfig, DwsTimeoutConfig, etc.
    /// </remarks>
    public required string ConfigurationType { get; init; }
    
    /// <summary>
    /// 配置ID / Configuration ID
    /// </summary>
    public required string ConfigurationId { get; init; }
    
    /// <summary>
    /// 操作类型 / Operation Type
    /// </summary>
    /// <remarks>
    /// 值: Create（创建）、Update（更新）、Delete（删除）
    /// Values: Create, Update, Delete
    /// </remarks>
    public required string OperationType { get; init; }
    
    /// <summary>
    /// 变更前的配置内容（JSON格式）/ Configuration Content Before Change (JSON)
    /// </summary>
    /// <remarks>
    /// 对于新建操作，此字段为 null
    /// For Create operations, this field is null
    /// </remarks>
    public string? ContentBefore { get; init; }
    
    /// <summary>
    /// 变更后的配置内容（JSON格式）/ Configuration Content After Change (JSON)
    /// </summary>
    /// <remarks>
    /// 对于删除操作，此字段为 null
    /// For Delete operations, this field is null
    /// </remarks>
    public string? ContentAfter { get; init; }
    
    /// <summary>
    /// 变更原因 / Change Reason
    /// </summary>
    public string? ChangeReason { get; init; }
    
    /// <summary>
    /// 操作用户 / Operator User
    /// </summary>
    /// <remarks>
    /// 记录执行操作的用户标识，可以是用户名、用户ID或系统标识
    /// Records the user identifier who performed the operation
    /// </remarks>
    public string? OperatorUser { get; init; }
    
    /// <summary>
    /// 操作IP地址 / Operator IP Address
    /// </summary>
    public string? OperatorIpAddress { get; init; }
    
    /// <summary>
    /// 创建时间（变更发生的时间）/ Created At (Time when change occurred)
    /// </summary>
    public required DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 备注信息 / Remarks
    /// </summary>
    public string? Remarks { get; init; }
}
