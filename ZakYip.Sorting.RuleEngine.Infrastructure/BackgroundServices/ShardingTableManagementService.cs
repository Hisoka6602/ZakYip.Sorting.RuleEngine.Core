using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 分片表自动管理服务
/// Background service for automatic sharding table management
/// </summary>
public class ShardingTableManagementService : BackgroundService
{
    private readonly ILogger<ShardingTableManagementService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ShardingSettings _settings;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public ShardingTableManagementService(
        ILogger<ShardingTableManagementService> logger,
        IServiceProvider serviceProvider,
        IOptions<ShardingSettings> settings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("分片功能未启用，分片表管理服务将不运行");
            return;
        }

        _logger.LogInformation(
            "分片表管理服务已启动，策略: {Strategy}",
            _settings.Strategy);

        // 首次启动时立即检查并创建表
        await EnsureTablesExistAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await EnsureTablesExistAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查和创建分片表时发生错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("分片表管理服务已停止");
    }

    /// <summary>
    /// 确保必要的分片表存在
    /// Ensure necessary sharding tables exist
    /// </summary>
    private async Task EnsureTablesExistAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var shardedContext = scope.ServiceProvider.GetService<ShardedLogDbContext>();

        if (shardedContext == null)
        {
            _logger.LogWarning("无法获取分片数据库上下文");
            return;
        }

        try
        {
            // 确保数据库存在
            await shardedContext.Database.EnsureCreatedAsync(cancellationToken);

            // 根据策略确定需要创建的表
            var tablesToCheck = GetTableNamesToCheck();

            foreach (var tableName in tablesToCheck)
            {
                await EnsureTableExistsAsync(shardedContext, tableName, cancellationToken);
            }

            _logger.LogInformation("分片表检查完成，已检查 {Count} 个表", tablesToCheck.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保分片表存在时发生错误");
        }
    }

    /// <summary>
    /// 根据分片策略获取需要检查的表名列表
    /// Get list of table names to check based on sharding strategy
    /// </summary>
    private List<string> GetTableNamesToCheck()
    {
        var tables = new List<string>();
        var now = DateTime.UtcNow;

        switch (_settings.Strategy.ToLower())
        {
            case "daily":
                // 检查当前日期和未来7天的表
                for (int i = 0; i < 8; i++)
                {
                    var date = now.AddDays(i);
                    tables.Add($"parcel_log_entries_{date:yyyyMMdd}");
                }
                break;

            case "weekly":
                // 检查当前周和未来4周的表
                for (int i = 0; i < 5; i++)
                {
                    var date = now.AddDays(i * 7);
                    var weekOfYear = GetIso8601WeekOfYear(date);
                    tables.Add($"parcel_log_entries_{date.Year}W{weekOfYear:D2}");
                }
                break;

            case "monthly":
            default:
                // 检查当前月和未来3个月的表
                for (int i = 0; i < 4; i++)
                {
                    var date = now.AddMonths(i);
                    tables.Add($"parcel_log_entries_{date:yyyyMM}");
                }
                break;
        }

        return tables;
    }

    /// <summary>
    /// 确保指定的表存在
    /// Ensure the specified table exists
    /// </summary>
    private async Task EnsureTableExistsAsync(ShardedLogDbContext context, string tableName, CancellationToken cancellationToken)
    {
        try
        {
            // 检查表是否存在
            var sql = $@"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = '{tableName}'";

            var exists = await context.Database.SqlQueryRaw<int>(sql).FirstOrDefaultAsync(cancellationToken);

            if (exists == 0)
            {
                // 创建表（使用与ParcelLogEntry相同的结构）
                var createTableSql = $@"
                    CREATE TABLE IF NOT EXISTS `{tableName}` (
                        `Id` char(36) NOT NULL,
                        `ParcelId` varchar(100) NOT NULL,
                        `CartNumber` varchar(100) DEFAULT NULL,
                        `ChuteNumber` varchar(100) DEFAULT NULL,
                        `Status` varchar(50) DEFAULT NULL,
                        `Weight` decimal(18,2) DEFAULT NULL,
                        `Volume` decimal(18,2) DEFAULT NULL,
                        `ProcessingTimeMs` int NOT NULL,
                        `CreatedAt` datetime(6) NOT NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_{tableName}_CreatedAt` (`CreatedAt` DESC),
                        KEY `IX_{tableName}_ParcelId` (`ParcelId`),
                        KEY `IX_{tableName}_ParcelId_CreatedAt` (`ParcelId`, `CreatedAt`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                await context.Database.ExecuteSqlRawAsync(createTableSql, cancellationToken);
                _logger.LogInformation("已创建分片表: {TableName}", tableName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查或创建表 {TableName} 时发生错误", tableName);
        }
    }

    /// <summary>
    /// 获取ISO 8601标准的周数
    /// Get ISO 8601 week number
    /// </summary>
    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
        if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }

        return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            System.DayOfWeek.Monday);
    }
}
