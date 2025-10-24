using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.MySql;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 数据清理后台服务
/// Background service for automatic data cleanup based on retention policy
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly ILogger<DataCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShardingSettings _settings;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public DataCleanupService(
        ILogger<DataCleanupService> logger,
        IServiceProvider serviceProvider,
        IOptions<ShardingSettings> settings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("数据清理服务已启动，保留期: {RetentionDays}天", _settings.RetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
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
        // 检查是否应该执行清理（基于时间）
        var now = DateTime.UtcNow;
        if (now.Hour != 2) // 只在凌晨2点执行
        {
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
                    var parcelDeletedCount = await shardedContext.ParcelLogEntries
                        .Where(e => e.CreatedAt < cutoffDate)
                        .ExecuteDeleteAsync(cancellationToken);

                    _logger.LogInformation("已删除 {Count} 条旧包裹日志记录",
                        parcelDeletedCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理数据时发生错误");
        }
    }
}
