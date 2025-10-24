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
/// Background service for archiving hot data to cold storage
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
        var now = DateTime.UtcNow;
        if (now.Hour != 3) // 只在凌晨3点执行
        {
            return;
        }

        if (!_settings.Enabled)
        {
            return;
        }

        _logger.LogInformation("开始执行数据归档...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MySqlLogDbContext>();

        if (dbContext == null)
        {
            _logger.LogWarning("无法获取数据库上下文，跳过归档");
            return;
        }

        var coldDataThreshold = DateTime.UtcNow.AddDays(-_settings.ColdDataThresholdDays);

        try
        {
            // 统计需要归档的数据
            var hotDataCount = await dbContext.LogEntries
                .Where(e => e.CreatedAt >= coldDataThreshold)
                .CountAsync(cancellationToken);

            var coldDataCount = await dbContext.LogEntries
                .Where(e => e.CreatedAt < coldDataThreshold)
                .CountAsync(cancellationToken);

            _logger.LogInformation(
                "数据统计 - 热数据: {HotCount} 条, 冷数据: {ColdCount} 条",
                hotDataCount, coldDataCount);

            // 在实际应用中，这里应该将冷数据移动到冷存储表或分区
            // In production, move cold data to cold storage tables or partitions
            
            // 示例：标记冷数据（可以添加一个IsCold字段）
            // Example: Mark cold data (could add an IsCold field)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "归档数据时发生错误");
        }
    }
}
