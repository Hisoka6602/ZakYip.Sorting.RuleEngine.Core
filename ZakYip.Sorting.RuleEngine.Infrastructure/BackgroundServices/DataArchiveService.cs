using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 数据归档后台服务
/// </summary>
public class DataArchiveService : BackgroundService
{
    private readonly ILogger<DataArchiveService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShardingSettings _settings;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public DataArchiveService(
        ILogger<DataArchiveService> logger,
        IServiceProvider serviceProvider,
        IOptions<ShardingSettings> settings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("数据归档服务已启动，冷数据阈值: {ColdDataThresholdDays}天",
            _settings.ColdDataThresholdDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformArchiveAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据归档过程中发生错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("数据归档服务已停止");
    }

    private async Task PerformArchiveAsync(CancellationToken cancellationToken)
    {
        // 检查是否应该执行归档（基于时间）
        var now = DateTime.Now;
        if (now.Hour != 3) // 只在凌晨3点执行
        {
            return;
        }

        if (!_settings.Enabled)
        {
            return;
        }

        _logger.LogInformation("开始执行数据归档...");
        var startTime = DateTime.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();

        if (dbContext == null)
        {
            _logger.LogWarning("无法获取数据库上下文，跳过归档");
            return;
        }

        // 检查数据库连接是否可用
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogWarning("数据库连接不可用，跳过归档");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查数据库连接时发生错误，跳过归档");
            return;
        }

        var coldDataThreshold = DateTime.Now.AddDays(-_settings.ColdDataThresholdDays);

        try
        {
            // 统计需要归档的数据 - 并行执行多个统计查询
            var statisticsTasks = new[]
            {
                CountRecordsAsync(dbContext, coldDataThreshold, true, cancellationToken), // 热数据
                CountRecordsAsync(dbContext, coldDataThreshold, false, cancellationToken), // 冷数据
            };

            var results = await Task.WhenAll(statisticsTasks);
            var hotDataCount = results[0];
            var coldDataCount = results[1];

            _logger.LogInformation(
                "数据统计 - 热数据: {HotCount} 条, 冷数据: {ColdCount} 条",
                hotDataCount, coldDataCount);

            if (coldDataCount > 0)
            {
                // 使用批量处理归档冷数据
                await ArchiveColdDataInBatchesAsync(dbContext, coldDataThreshold, coldDataCount, cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("数据归档完成，耗时: {Duration}秒", duration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "归档数据时发生错误");
        }
    }

    /// <summary>
    /// 统计记录数量
    /// </summary>
    private async Task<int> CountRecordsAsync(
        MySqlLogDbContext dbContext, 
        DateTime threshold, 
        bool isHot, 
        CancellationToken cancellationToken)
    {
        try
        {
            var query = isHot
                ? dbContext.LogEntries.Where(e => e.CreatedAt >= threshold)
                : dbContext.LogEntries.Where(e => e.CreatedAt < threshold);

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统计{DataType}数据时发生错误", isHot ? "热" : "冷");
            return 0;
        }
    }

    /// <summary>
    /// 批量归档冷数据
    /// </summary>
    private async Task ArchiveColdDataInBatchesAsync(
        MySqlLogDbContext dbContext,
        DateTime threshold,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var batchSize = _settings.ArchiveBatchSize; // 使用配置的批次大小
        var batchDelayMs = _settings.ArchiveBatchDelayMs; // 使用配置的批次延迟
        var failureThreshold = _settings.ArchiveFailureThreshold; // 使用配置的失败阈值
        var processedCount = 0;
        var failedCount = 0;

        _logger.LogInformation("开始批量归档 {TotalCount} 条冷数据，批次大小: {BatchSize}，批次延迟: {DelayMs}ms", 
            totalCount, batchSize, batchDelayMs);

        while (processedCount < totalCount && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 获取一批需要归档的记录ID
                var batchIds = await dbContext.LogEntries
                    .Where(e => e.CreatedAt < threshold)
                    .OrderBy(e => e.CreatedAt)
                    .Take(batchSize)
                    .Select(e => e.Id)
                    .ToListAsync(cancellationToken);

                if (!batchIds.Any())
                    break;

                // 在实际应用中，这里应该：
                // 1. 将这批数据复制到归档表
                // 2. 验证复制成功
                // 3. 从主表删除这批数据
                // 示例代码（需要根据实际情况调整）:
                /*
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 复制到归档表
                    var recordsToArchive = await dbContext.LogEntries
                        .Where(e => batchIds.Contains(e.Id))
                        .ToListAsync(cancellationToken);
                    
                    await dbContext.ArchivedLogEntries.AddRangeAsync(recordsToArchive, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    // 从主表删除
                    dbContext.LogEntries.RemoveRange(recordsToArchive);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    await transaction.CommitAsync(cancellationToken);
                    
                    processedCount += batchIds.Count;
                    _logger.LogInformation("已归档 {ProcessedCount}/{TotalCount} 条记录", processedCount, totalCount);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    failedCount += batchIds.Count;
                    _logger.LogError(ex, "归档批次失败，跳过 {Count} 条记录", batchIds.Count);
                }
                */

                // 暂时只记录日志，不实际移动数据
                processedCount += batchIds.Count;
                _logger.LogInformation("处理进度: {ProcessedCount}/{TotalCount} ({Percentage:F1}%)", 
                    processedCount, totalCount, (processedCount * 100.0 / totalCount));

                // 避免对数据库造成过大压力，批次之间稍作延迟
                await Task.Delay(batchDelayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理归档批次时发生错误");
                failedCount++;
                
                // 如果连续失败太多次，停止归档
                if (failedCount > failureThreshold)
                {
                    _logger.LogError("归档失败次数超过阈值({Threshold})，停止归档操作", failureThreshold);
                    break;
                }
            }
        }

        _logger.LogInformation("批量归档完成，成功: {ProcessedCount} 条, 失败: {FailedCount} 条", 
            processedCount, failedCount);
    }
}
