using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 甘特图服务实现
/// </summary>
public class GanttChartService : IGanttChartService
{
    private readonly MySqlLogDbContext? _mysqlContext;
    private readonly SqliteLogDbContext? _sqliteContext;
    private readonly ILogger<GanttChartService> _logger;

    public GanttChartService(
        MySqlLogDbContext? mysqlContext,
        SqliteLogDbContext? sqliteContext,
        ILogger<GanttChartService> logger)
    {
        _mysqlContext = mysqlContext;
        _sqliteContext = sqliteContext;
        _logger = logger;
    }

    /// <summary>
    /// 查询指定包裹前后N条数据的甘特图数据
    /// </summary>
    public async Task<GanttChartQueryResponse> QueryGanttChartDataAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 步骤1：验证参数
            if (string.IsNullOrWhiteSpace(target))
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = "目标包裹ID或条码不能为空"
                };
            }

            if (beforeCount < 0 || beforeCount > 100)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = "查询前面数据条数必须在0到100之间"
                };
            }

            if (afterCount < 0 || afterCount > 100)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = "查询后面数据条数必须在0到100之间"
                };
            }

            // 步骤2：查询数据（优先使用MySQL）
            var items = await QueryDataAsync(target, beforeCount, afterCount, cancellationToken);

            if (items.Count == 0)
            {
                return new GanttChartQueryResponse
                {
                    Success = false,
                    ErrorMessage = $"未找到目标包裹: {target}"
                };
            }

            // 步骤3：构建响应
            var targetItem = items.FirstOrDefault(x => x.IsTarget);
            var targetIndex = targetItem != null ? items.IndexOf(targetItem) : (int?)null;

            return new GanttChartQueryResponse
            {
                Items = items,
                TargetParcelId = targetItem?.ParcelId,
                TargetIndex = targetIndex,
                TotalCount = items.Count,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询甘特图数据失败: Target={Target}", target);
            return new GanttChartQueryResponse
            {
                Success = false,
                ErrorMessage = $"查询失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 从数据库查询数据
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryDataAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 优先使用MySQL
        if (_mysqlContext != null)
        {
            return await QueryFromMySqlAsync(target, beforeCount, afterCount, cancellationToken);
        }

        // 降级使用SQLite
        if (_sqliteContext != null)
        {
            return await QueryFromSqliteAsync(target, beforeCount, afterCount, cancellationToken);
        }

        return new List<GanttChartDataItem>();
    }

    /// <summary>
    /// 从MySQL数据库查询
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryFromMySqlAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 步骤1：查找目标包裹的匹配日志
        var targetLog = await _mysqlContext!.MatchingLogs
            .Where(m => m.ParcelId == target)
            .OrderByDescending(m => m.MatchingTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetLog == null)
        {
            return new List<GanttChartDataItem>();
        }

        var targetTime = targetLog.MatchingTime;

        // 步骤2：查询目标前面的N条数据
        var beforeLogs = await _mysqlContext.MatchingLogs
            .Where(m => m.MatchingTime < targetTime)
            .OrderByDescending(m => m.MatchingTime)
            .Take(beforeCount)
            .ToListAsync(cancellationToken);

        // 步骤3：查询目标后面的N条数据
        var afterLogs = await _mysqlContext.MatchingLogs
            .Where(m => m.MatchingTime > targetTime)
            .OrderBy(m => m.MatchingTime)
            .Take(afterCount)
            .ToListAsync(cancellationToken);

        // 步骤4：合并并按时间排序
        var allLogs = beforeLogs
            .OrderBy(m => m.MatchingTime)
            .Concat(new[] { targetLog })
            .Concat(afterLogs.OrderBy(m => m.MatchingTime))
            .ToList();

        // 步骤5：关联查询格口信息、DWS数据和API通信日志
        var result = new List<GanttChartDataItem>();
        int sequenceNumber = 1;

        foreach (var log in allLogs)
        {
            // 查询DWS通信日志
            var dwsLog = await _mysqlContext.DwsCommunicationLogs
                .Where(d => d.Barcode == log.ParcelId || d.Barcode != null && log.ParcelId.Contains(d.Barcode))
                .OrderByDescending(d => d.CommunicationTime)
                .FirstOrDefaultAsync(cancellationToken);

            // 查询API通信日志
            var apiLog = await _mysqlContext.ApiCommunicationLogs
                .Where(a => a.ParcelId == log.ParcelId)
                .OrderByDescending(a => a.RequestTime)
                .FirstOrDefaultAsync(cancellationToken);

            // 查询格口信息
            string? chuteCode = null;
            string? chuteName = null;
            if (log.ChuteId.HasValue)
            {
                var chute = await _mysqlContext.Chutes
                    .FirstOrDefaultAsync(c => c.ChuteId == log.ChuteId.Value, cancellationToken);
                chuteCode = chute?.ChuteCode;
                chuteName = chute?.ChuteName;
            }

            var item = new GanttChartDataItem
            {
                ParcelId = log.ParcelId,
                Barcode = dwsLog?.Barcode,
                MatchedRuleId = log.MatchedRuleId,
                ChuteId = log.ChuteId,
                ChuteCode = chuteCode,
                ChuteName = chuteName,
                MatchingTime = log.MatchingTime,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                DwsCommunicationTime = dwsLog?.CommunicationTime,
                ApiRequestTime = apiLog?.RequestTime,
                ApiDurationMs = apiLog?.DurationMs,
                Weight = dwsLog?.Weight,
                Volume = dwsLog?.Volume,
                CartOccupancy = log.CartOccupancy,
                SequenceNumber = sequenceNumber++,
                IsTarget = log.ParcelId == target
            };

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 从SQLite数据库查询（降级方案）
    /// </summary>
    private async Task<List<GanttChartDataItem>> QueryFromSqliteAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken)
    {
        // 实现与MySQL类似，只是使用SQLite上下文
        var targetLog = await _sqliteContext!.MatchingLogs
            .Where(m => m.ParcelId == target)
            .OrderByDescending(m => m.MatchingTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetLog == null)
        {
            return new List<GanttChartDataItem>();
        }

        var targetTime = targetLog.MatchingTime;

        var beforeLogs = await _sqliteContext.MatchingLogs
            .Where(m => m.MatchingTime < targetTime)
            .OrderByDescending(m => m.MatchingTime)
            .Take(beforeCount)
            .ToListAsync(cancellationToken);

        var afterLogs = await _sqliteContext.MatchingLogs
            .Where(m => m.MatchingTime > targetTime)
            .OrderBy(m => m.MatchingTime)
            .Take(afterCount)
            .ToListAsync(cancellationToken);

        var allLogs = beforeLogs
            .OrderBy(m => m.MatchingTime)
            .Concat(new[] { targetLog })
            .Concat(afterLogs.OrderBy(m => m.MatchingTime))
            .ToList();

        var result = new List<GanttChartDataItem>();
        int sequenceNumber = 1;

        foreach (var log in allLogs)
        {
            var dwsLog = await _sqliteContext.DwsCommunicationLogs
                .Where(d => d.Barcode == log.ParcelId || d.Barcode != null && log.ParcelId.Contains(d.Barcode))
                .OrderByDescending(d => d.CommunicationTime)
                .FirstOrDefaultAsync(cancellationToken);

            var apiLog = await _sqliteContext.ApiCommunicationLogs
                .Where(a => a.ParcelId == log.ParcelId)
                .OrderByDescending(a => a.RequestTime)
                .FirstOrDefaultAsync(cancellationToken);

            string? chuteCode = null;
            string? chuteName = null;
            if (log.ChuteId.HasValue)
            {
                var chute = await _sqliteContext.Chutes
                    .FirstOrDefaultAsync(c => c.ChuteId == log.ChuteId.Value, cancellationToken);
                chuteCode = chute?.ChuteCode;
                chuteName = chute?.ChuteName;
            }

            var item = new GanttChartDataItem
            {
                ParcelId = log.ParcelId,
                Barcode = dwsLog?.Barcode,
                MatchedRuleId = log.MatchedRuleId,
                ChuteId = log.ChuteId,
                ChuteCode = chuteCode,
                ChuteName = chuteName,
                MatchingTime = log.MatchingTime,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                DwsCommunicationTime = dwsLog?.CommunicationTime,
                ApiRequestTime = apiLog?.RequestTime,
                ApiDurationMs = apiLog?.DurationMs,
                Weight = dwsLog?.Weight,
                Volume = dwsLog?.Volume,
                CartOccupancy = log.CartOccupancy,
                SequenceNumber = sequenceNumber++,
                IsTarget = log.ParcelId == target
            };

            result.Add(item);
        }

        return result;
    }
}
