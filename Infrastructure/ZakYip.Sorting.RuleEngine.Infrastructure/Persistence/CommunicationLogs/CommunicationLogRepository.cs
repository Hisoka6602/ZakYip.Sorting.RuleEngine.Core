using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.CommunicationLogs;

/// <summary>
/// 通信日志仓储实现
/// </summary>
public class CommunicationLogRepository : ICommunicationLogRepository
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly MySqlLogDbContext _dbContext;
    private readonly ILogger<CommunicationLogRepository> _logger;

    public CommunicationLogRepository(
        MySqlLogDbContext dbContext,
        ILogger<CommunicationLogRepository> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_dbContext = dbContext;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 记录通信日志
    /// </summary>
    public async Task LogCommunicationAsync(
        CommunicationType type,
        CommunicationDirection direction,
        string message,
        string? parcelId = null,
        string? remoteAddress = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        try
        {
            var log = new CommunicationLog
            {
                CommunicationType = type,
                Direction = direction,
                Message = message,
                ParcelId = parcelId,
                RemoteAddress = remoteAddress,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CreatedAt = _clock.LocalNow
            };

            _dbContext.CommunicationLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("通信日志已记录: {Type} {Direction} - {Message}", type, direction, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录通信日志失败: {Type} {Direction}", type, direction);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 获取通信日志
    /// </summary>
    public async Task<List<CommunicationLog>> GetLogsAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CommunicationType? type = null,
        string? parcelId = null,
        int maxRecords = 1000)
    {
        try
        {
            var query = _dbContext.CommunicationLogs.AsQueryable();

            if (startTime.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= endTime.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(x => x.CommunicationType == type.Value);
            }

            if (!string.IsNullOrEmpty(parcelId))
            {
                query = query.Where(x => x.ParcelId == parcelId);
            }

            return await query
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(maxRecords)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通信日志失败");
            return new List<CommunicationLog>();
        }
    }
}
