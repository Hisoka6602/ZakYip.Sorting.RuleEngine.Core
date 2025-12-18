using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 配置审计日志仓储接口 / Configuration Audit Log Repository Interface
/// </summary>
public interface IConfigurationAuditLogRepository
{
    /// <summary>
    /// 添加审计日志 / Add audit log
    /// </summary>
    /// <param name="auditLog">审计日志实体 / Audit log entity</param>
    /// <returns>是否成功 / Success or not</returns>
    Task<bool> AddAsync(ConfigurationAuditLog auditLog);
    
    /// <summary>
    /// 根据配置类型和ID获取审计日志 / Get audit logs by configuration type and ID
    /// </summary>
    /// <param name="configurationType">配置类型 / Configuration type</param>
    /// <param name="configurationId">配置ID / Configuration ID</param>
    /// <param name="pageSize">页大小 / Page size</param>
    /// <param name="pageNumber">页码 / Page number</param>
    /// <returns>审计日志列表 / Audit log list</returns>
    Task<IEnumerable<ConfigurationAuditLog>> GetByConfigurationAsync(
        string configurationType, 
        long configurationId,
        int pageSize = 50,
        int pageNumber = 1);
    
    /// <summary>
    /// 根据时间范围获取审计日志 / Get audit logs by time range
    /// </summary>
    /// <param name="startTime">开始时间 / Start time</param>
    /// <param name="endTime">结束时间 / End time</param>
    /// <param name="configurationType">配置类型（可选）/ Configuration type (optional)</param>
    /// <param name="pageSize">页大小 / Page size</param>
    /// <param name="pageNumber">页码 / Page number</param>
    /// <returns>审计日志列表 / Audit log list</returns>
    Task<IEnumerable<ConfigurationAuditLog>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        string? configurationType = null,
        int pageSize = 50,
        int pageNumber = 1);
    
    /// <summary>
    /// 获取最近的审计日志 / Get recent audit logs
    /// </summary>
    /// <param name="count">获取数量 / Count</param>
    /// <param name="configurationType">配置类型（可选）/ Configuration type (optional)</param>
    /// <returns>审计日志列表 / Audit log list</returns>
    Task<IEnumerable<ConfigurationAuditLog>> GetRecentAsync(int count = 100, string? configurationType = null);
}
