using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// MySQL自动调谐服务
/// </summary>
public class MySqlAutoTuningService : BackgroundService
{
    private readonly ILogger<MySqlAutoTuningService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public MySqlAutoTuningService(
        ILogger<MySqlAutoTuningService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MySQL自动调谐服务已启动");

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
                _logger.LogError(ex, "MySQL自动调谐过程中发生错误");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("MySQL自动调谐服务已停止");
    }

    private async Task PerformAutoTuningAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行MySQL自动调谐...");

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

            _logger.LogInformation("MySQL自动调谐完成");
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
            var tableStats = await dbContext.Database.SqlQueryRaw<TableStatistics>(@"
                SELECT 
                    table_name as TableName,
                    table_rows as RowCount,
                    ROUND((data_length + index_length) / 1024 / 1024, 2) as SizeMB
                FROM information_schema.TABLES
                WHERE table_schema = DATABASE()
                ORDER BY (data_length + index_length) DESC
            ").ToListAsync(cancellationToken);

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
            // 查询未使用的索引
            var unusedIndexes = await dbContext.Database.SqlQueryRaw<IndexUsageInfo>(@"
                SELECT 
                    OBJECT_SCHEMA as DatabaseName,
                    OBJECT_NAME as TableName,
                    INDEX_NAME as IndexName
                FROM performance_schema.table_io_waits_summary_by_index_usage
                WHERE INDEX_NAME IS NOT NULL
                AND INDEX_NAME != 'PRIMARY'
                AND COUNT_STAR = 0
                AND OBJECT_SCHEMA = DATABASE()
            ").ToListAsync(cancellationToken);

            foreach (var index in unusedIndexes)
            {
                _logger.LogWarning(
                    "发现未使用的索引 - 表: {TableName}, 索引: {IndexName}",
                    index.TableName, index.IndexName);
            }

            if (unusedIndexes.Count == 0)
            {
                _logger.LogInformation("所有索引都在使用中");
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
            var connectionStatus = await dbContext.Database.SqlQueryRaw<ConnectionStatus>(@"
                SHOW STATUS WHERE Variable_name IN (
                    'Threads_connected', 
                    'Threads_running', 
                    'Max_used_connections',
                    'Aborted_connects'
                )
            ").ToListAsync(cancellationToken);

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
            var slowQueries = await dbContext.Database.SqlQueryRaw<SlowQueryInfo>(@"
                SELECT 
                    COUNT(*) as Count
                FROM information_schema.processlist
                WHERE time > 5
                AND command != 'Sleep'
            ").FirstOrDefaultAsync(cancellationToken);

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
