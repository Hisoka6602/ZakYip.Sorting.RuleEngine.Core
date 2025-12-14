using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// .log文件自动清理后台服务
/// </summary>
public class LogFileCleanupService : BackgroundService
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILogger<LogFileCleanupService> _logger;
    private readonly LogFileCleanupSettings _settings;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // 每小时检查一次
    private const decimal BytesPerMB = 1024.0m * 1024.0m; // 字节到MB的转换常量

    public LogFileCleanupService(
        ILogger<LogFileCleanupService> logger,
        IOptions<LogFileCleanupSettings> settings,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_logger = logger;
        _settings = settings.Value;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("日志文件清理服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupLogFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理日志文件时发生错误");
            }

            // 等待下一次检查
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("日志文件清理服务已停止");
    }

    /// <summary>
    /// 清理过期的.log文件
    /// </summary>
    private async Task CleanupLogFilesAsync(CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            // 清理消息仅在控制台输出
            Console.WriteLine("日志文件清理功能未启用");
            return;
        }

        var retentionDays = _settings.RetentionDays;
        var logDirectory = _settings.LogDirectory ?? "./logs";
        
        // 确保路径是绝对路径
        if (!Path.IsPathRooted(logDirectory))
        {
            logDirectory = Path.Combine(AppContext.BaseDirectory, logDirectory);
        }

        if (!Directory.Exists(logDirectory))
        {
            Console.WriteLine($"日志目录不存在: {logDirectory}");
            return;
        }

        Console.WriteLine($"开始清理日志文件，目录: {logDirectory}，保留天数: {retentionDays}天");

        var cutoffDate = _clock.LocalNow.AddDays(-retentionDays);
        var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories);
        var deletedCount = 0;
        var totalSize = 0L;

        foreach (var logFile in logFiles)
        {
            try
            {
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    totalSize += fileInfo.Length;
                    fileInfo.Delete();
                    deletedCount++;
                    // 清理消息仅在控制台输出，不记录到logs
                    Console.WriteLine($"已删除过期日志文件: {logFile}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除日志文件失败: {File}", logFile);
            }

            if (cancellationToken.IsCancellationRequested)
                break;
        }

        if (deletedCount > 0)
        {
            var sizeMB = totalSize / BytesPerMB;
            // 清理消息仅在控制台输出，不记录到logs
            Console.WriteLine($"日志文件清理完成，共删除 {deletedCount} 个文件，释放空间 {sizeMB:F2} MB");
        }
        else
        {
            Console.WriteLine("没有需要清理的日志文件");
        }

        await Task.CompletedTask;
    }
}
