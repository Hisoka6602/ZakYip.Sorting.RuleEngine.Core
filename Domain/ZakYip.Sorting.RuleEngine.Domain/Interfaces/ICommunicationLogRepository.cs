using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 通信日志仓储接口
/// </summary>
public interface ICommunicationLogRepository
{
    /// <summary>
    /// 记录通信日志
    /// </summary>
    Task LogCommunicationAsync(
        CommunicationType type,
        CommunicationDirection direction,
        string message,
        string? parcelId = null,
        string? remoteAddress = null,
        bool isSuccess = true,
        string? errorMessage = null);
    
    /// <summary>
    /// 获取通信日志
    /// </summary>
    Task<List<CommunicationLog>> GetLogsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CommunicationType? type = null,
        string? parcelId = null,
        int maxRecords = PaginationDefaults.MaxRecords);
}
