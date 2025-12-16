using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.ApiCommunicationLogs;

/// <summary>
/// API通信日志仓储实现
/// API Communication Log Repository Implementation
/// </summary>
public class ApiCommunicationLogRepository : IApiCommunicationLogRepository
{
    private readonly ILogger<ApiCommunicationLogRepository> _logger;
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;

    public ApiCommunicationLogRepository(
        ILogger<ApiCommunicationLogRepository> logger,
        MySqlLogDbContext? mysqlContext = null,
        SqliteLogDbContext? sqliteContext = null)
    {
        _logger = logger;
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
    }

    /// <summary>
    /// 保存API通信日志
    /// </summary>
    public async Task SaveAsync(ApiCommunicationLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先保存到MySQL（如果可用）
            if (_mysqlContext != null)
            {
                _mysqlContext.ApiCommunicationLogs.Add(log);
                await _mysqlContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("API通信日志已保存到MySQL: ParcelId={ParcelId}", log.ParcelId);
            }
            // 否则保存到SQLite
            else if (_sqliteContext != null)
            {
                _sqliteContext.ApiCommunicationLogs.Add(log);
                await _sqliteContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("API通信日志已保存到SQLite: ParcelId={ParcelId}", log.ParcelId);
            }
            else
            {
                _logger.LogWarning("未配置数据库上下文，无法保存API通信日志: ParcelId={ParcelId}", log.ParcelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存API通信日志失败: ParcelId={ParcelId}", log.ParcelId);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 批量保存API通信日志
    /// </summary>
    public async Task SaveManyAsync(IEnumerable<ApiCommunicationLog> logs, CancellationToken cancellationToken = default)
    {
        try
        {
            var logList = logs.ToList();
            if (!logList.Any())
            {
                return;
            }

            // 优先保存到MySQL（如果可用）
            if (_mysqlContext != null)
            {
                _mysqlContext.ApiCommunicationLogs.AddRange(logList);
                await _mysqlContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("批量保存{Count}条API通信日志到MySQL", logList.Count);
            }
            // 否则保存到SQLite
            else if (_sqliteContext != null)
            {
                _sqliteContext.ApiCommunicationLogs.AddRange(logList);
                await _sqliteContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("批量保存{Count}条API通信日志到SQLite", logList.Count);
            }
            else
            {
                _logger.LogWarning("未配置数据库上下文，无法批量保存API通信日志");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量保存API通信日志失败");
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 获取指定包裹的API通信日志
    /// </summary>
    public async Task<List<ApiCommunicationLog>> GetByParcelIdAsync(string parcelId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先从MySQL读取（如果可用）
            if (_mysqlContext != null)
            {
                return await _mysqlContext.ApiCommunicationLogs
                    .AsNoTracking()
                    .Where(log => log.ParcelId == parcelId)
                    .OrderByDescending(log => log.RequestTime)
                    .ToListAsync(cancellationToken);
            }
            // 否则从SQLite读取
            else if (_sqliteContext != null)
            {
                return await _sqliteContext.ApiCommunicationLogs
                    .AsNoTracking()
                    .Where(log => log.ParcelId == parcelId)
                    .OrderByDescending(log => log.RequestTime)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("未配置数据库上下文，无法查询API通信日志");
                return new List<ApiCommunicationLog>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询API通信日志失败: ParcelId={ParcelId}", parcelId);
            return new List<ApiCommunicationLog>();
        }
    }

    /// <summary>
    /// 获取指定时间范围内的API通信日志
    /// </summary>
    public async Task<List<ApiCommunicationLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先从MySQL读取（如果可用）
            if (_mysqlContext != null)
            {
                return await _mysqlContext.ApiCommunicationLogs
                    .AsNoTracking()
                    .Where(log => log.RequestTime >= startTime && log.RequestTime <= endTime)
                    .OrderByDescending(log => log.RequestTime)
                    .ToListAsync(cancellationToken);
            }
            // 否则从SQLite读取
            else if (_sqliteContext != null)
            {
                return await _sqliteContext.ApiCommunicationLogs
                    .AsNoTracking()
                    .Where(log => log.RequestTime >= startTime && log.RequestTime <= endTime)
                    .OrderByDescending(log => log.RequestTime)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning("未配置数据库上下文，无法查询API通信日志");
                return new List<ApiCommunicationLog>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询API通信日志失败: StartTime={StartTime}, EndTime={EndTime}", startTime, endTime);
            return new List<ApiCommunicationLog>();
        }
    }
}
