using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Dialects;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 数据库自动调谐服务
/// Database auto-tuning service
/// </summary>
public class MySqlAutoTuningService : BackgroundService
{
    private readonly ILogger<MySqlAutoTuningService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatabaseDialect _dialect;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public MySqlAutoTuningService(
        ILogger<MySqlAutoTuningService> logger,
        IServiceProvider serviceProvider,
        IDatabaseDialect dialect)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dialect = dialect;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 如果数据库不支持性能监控，跳过
        if (!_dialect.SupportsPerformanceMonitoring)
        {
            _logger.LogInformation("当前数据库不支持性能监控，自动调谐服务将不运行");
            return;
        }

        _logger.LogInformation("数据库自动调谐服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformAutoTuningAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库自动调谐过程中发生错误");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("数据库自动调谐服务已停止");
    }

    private async Task PerformAutoTuningAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行数据库自动调谐...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();

        if (dbContext == null)
        {
            _logger.LogWarning("无法获取数据库上下文，跳过调谐");
            return;
        }

        // 检查数据库连接是否可用
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogWarning("数据库连接不可用，跳过调谐");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查数据库连接时发生错误，跳过调谐");
            return;
        }

        try
        {
            // 1. 分析表统计信息
            await AnalyzeTableStatisticsAsync(dbContext, cancellationToken);

            // 2. 检查索引使用情况
            await AnalyzeIndexUsageAsync(dbContext, cancellationToken);

            // 3. 检查连接池状态
            await AnalyzeConnectionPoolAsync(dbContext, cancellationToken);

            // 4. 优化查询缓存
            await OptimizeQueryCacheAsync(dbContext, cancellationToken);

            _logger.LogInformation("数据库自动调谐完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行自动调谐时发生错误");
        }
    }

    private async Task AnalyzeTableStatisticsAsync(MySqlLogDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // 获取表统计信息
            var sql = _dialect.GetTableStatisticsQuery();
            var tableStats = await dbContext.Database.SqlQueryRaw<TableStatistics>(sql).ToListAsync(cancellationToken);

            foreach (var stat in tableStats)
            {
                _logger.LogInformation(
                    "表统计 - 表名: {TableName}, 行数: {RowCount}, 大小: {SizeMB}MB",
                    stat.TableName, stat.RowCount, stat.SizeMB);

                // 如果表行数超过100万且没有定期优化，建议优化
                if (stat.RowCount > 1000000)
                {
                    _logger.LogWarning(
                        "表 {TableName} 行数较多 ({RowCount})，建议定期优化或分区",
                        stat.TableName, stat.RowCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析表统计信息失败");
        }
    }

    private async Task AnalyzeIndexUsageAsync(MySqlLogDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // 先获取表统计信息以了解行数
            var tableStatsSql = _dialect.GetTableStatisticsQuery();
            var tableStats = await dbContext.Database.SqlQueryRaw<TableStatistics>(tableStatsSql).ToListAsync(cancellationToken);
            var tableRowCounts = tableStats.ToDictionary(t => t.TableName, t => t.RowCount);
            
            // 查询未使用的索引
            var sql = _dialect.GetIndexUsageQuery();
            var unusedIndexes = await dbContext.Database.SqlQueryRaw<IndexUsageInfo>(sql).ToListAsync(cancellationToken);

            var filteredIndexes = unusedIndexes.Where(index =>
            {
                // 获取表的行数
                var rowCount = tableRowCounts.TryGetValue(index.TableName, out var count) ? count : 0;
                
                // 过滤条件：行数小于1000的表，降级为Debug
                if (rowCount < 1000)
                    return false;
                
                // 过滤条件：索引名包含ParcelId、CreatedAt或_Desc的索引，降级为Debug
                if (index.IndexName.Contains("ParcelId", StringComparison.OrdinalIgnoreCase) ||
                    index.IndexName.Contains("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
                    index.IndexName.Contains("_Desc", StringComparison.OrdinalIgnoreCase))
                    return false;
                
                return true;
            }).ToList();

            foreach (var index in filteredIndexes)
            {
                _logger.LogWarning(
                    "发现未使用的索引 - 表: {TableName}, 索引: {IndexName}",
                    index.TableName, index.IndexName);
            }
            
            // 记录被过滤掉的索引到Debug级别
            var debugIndexes = unusedIndexes.Except(filteredIndexes).ToList();
            foreach (var index in debugIndexes)
            {
                var rowCount = tableRowCounts.TryGetValue(index.TableName, out var count) ? count : 0;
                _logger.LogDebug(
                    "未使用的索引（已过滤）- 表: {TableName}, 索引: {IndexName}, 行数: {RowCount}",
                    index.TableName, index.IndexName, rowCount);
            }

            if (filteredIndexes.Count == 0)
            {
                _logger.LogInformation("所有重要索引都在使用中（已过滤小表和常用索引）");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析索引使用情况失败");
        }
    }

    private async Task AnalyzeConnectionPoolAsync(MySqlLogDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // 获取连接池状态
            var sql = _dialect.GetConnectionStatusQuery();
            var connectionStatus = await dbContext.Database.SqlQueryRaw<ConnectionStatus>(sql).ToListAsync(cancellationToken);

            foreach (var status in connectionStatus)
            {
                _logger.LogInformation("连接池状态 - {Variable}: {Value}",
                    status.VariableName, status.Value);
            }

            // 检查是否需要调整连接池大小
            var threadsConnected = connectionStatus
                .FirstOrDefault(s => s.VariableName == "Threads_connected");
            
            if (threadsConnected != null && int.TryParse(threadsConnected.Value, out var connected))
            {
                if (connected > 100)
                {
                    _logger.LogWarning(
                        "当前连接数较高 ({Connected})，建议检查连接泄漏或增加连接池大小",
                        connected);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分析连接池状态失败");
        }
    }

    private async Task OptimizeQueryCacheAsync(MySqlLogDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // 获取慢查询统计
            var sql = _dialect.GetSlowQueryStatisticsQuery();
            var slowQueries = await dbContext.Database.SqlQueryRaw<SlowQueryInfo>(sql).SingleOrDefaultAsync(cancellationToken);

            if (slowQueries != null && slowQueries.Count > 0)
            {
                _logger.LogWarning(
                    "发现 {Count} 个慢查询（执行时间>5秒），建议优化",
                    slowQueries.Count);
            }
            else
            {
                _logger.LogInformation("当前没有慢查询");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "优化查询缓存失败");
        }
    }
}

/// <summary>
/// 表统计信息
/// </summary>
public class TableStatistics
{
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public decimal SizeMB { get; set; }
}

/// <summary>
/// 索引使用信息
/// </summary>
public class IndexUsageInfo
{
    public string DatabaseName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
}

/// <summary>
/// 连接状态信息
/// </summary>
public class ConnectionStatus
{
    [Column("Variable_name")]
    public string VariableName { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 慢查询信息
/// </summary>
public class SlowQueryInfo
{
    public int Count { get; set; }
}
