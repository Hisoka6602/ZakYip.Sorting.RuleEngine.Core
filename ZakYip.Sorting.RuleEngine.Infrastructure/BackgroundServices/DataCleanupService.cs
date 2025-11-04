using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 数据清理后台服务（基于空闲策略）
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly ILogger<DataCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShardingSettings _settings;
    private readonly IParcelActivityTracker _activityTracker;
    private DateTime? _lastCleanupTime;

    public DataCleanupService(
        ILogger<DataCleanupService> logger,
        IServiceProvider serviceProvider,
        IOptions<ShardingSettings> settings,
        IParcelActivityTracker activityTracker)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _activityTracker = activityTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "数据清理服务已启动（基于空闲策略），保留期: {RetentionDays}天，空闲阈值: {IdleMinutes}分钟",
            _settings.RetentionDays,
            _settings.IdleMinutesBeforeCleanup);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var checkInterval = TimeSpan.FromSeconds(_settings.IdleCheckIntervalSeconds);
                await Task.Delay(checkInterval, stoppingToken);

                // 检查是否处于空闲状态
                if (_activityTracker.IsIdle(_settings.IdleMinutesBeforeCleanup))
                {
                    var lastActivity = _activityTracker.GetLastActivityTime();
                    _logger.LogInformation(
                        "系统处于空闲状态，上次包裹创建时间: {LastActivity}，开始执行数据清理...",
                        lastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "从未创建");

                    await PerformCleanupAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据清理过程中发生错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("数据清理服务已停止");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        // 防止频繁清理（每次清理后至少间隔1小时）
        if (_lastCleanupTime.HasValue && 
            (DateTime.UtcNow - _lastCleanupTime.Value).TotalHours < 1)
        {
            _logger.LogDebug("距离上次清理不足1小时，跳过本次清理");
            return;
        }

        _logger.LogInformation("开始执行数据清理...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();

        if (dbContext == null)
        {
            _logger.LogWarning("无法获取数据库上下文，跳过清理");
            return;
        }

        // 检查数据库连接是否可用
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogWarning("数据库连接不可用，跳过清理");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查数据库连接时发生错误，跳过清理");
            return;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
        
        try
        {
            // 清理旧的日志条目
            var deletedCount = await dbContext.LogEntries
                .Where(e => e.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("已删除 {Count} 条旧日志记录（早于 {CutoffDate}）",
                deletedCount, cutoffDate);

            // 如果使用分片数据库，清理旧的包裹日志
            if (_settings.Enabled)
            {
                var shardedContext = scope.ServiceProvider.GetService<ShardedLogDbContext>();
                if (shardedContext != null)
                {
                    // 通过依赖注入获取表存在性检查器
                    var tableCheckerFactory = scope.ServiceProvider.GetRequiredService<Func<ShardedLogDbContext, ITableExistenceChecker>>();
                    var tableChecker = tableCheckerFactory(shardedContext);

                    // 检查ParcelLogEntries表是否存在
                    var tableExists = await tableChecker.TableExistsAsync("ParcelLogEntries", cancellationToken);
                    
                    if (tableExists)
                    {
                        var parcelDeletedCount = await shardedContext.ParcelLogEntries
                            .Where(e => e.CreatedAt < cutoffDate)
                            .ExecuteDeleteAsync(cancellationToken);

                        _logger.LogInformation("已删除 {Count} 条旧包裹日志记录",
                            parcelDeletedCount);
                    }
                    else
                    {
                        _logger.LogDebug("表 'ParcelLogEntries' 不存在，跳过包裹日志清理");
                    }
                }
            }

            _lastCleanupTime = DateTime.UtcNow;
            _logger.LogInformation("数据清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理数据时发生错误");
        }
    }
}
