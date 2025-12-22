using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// WCS API通信日志后台处理服务
/// Background service for processing WCS API communication logs
/// 使用 Channel 实现高性能、低线程消耗的异步日志处理
/// Uses Channel for high-performance, low-thread-consumption async log processing
/// </summary>
public class WcsApiLogBackgroundService : BackgroundService
{
    private readonly Channel<ApiCommunicationLog> _logChannel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WcsApiLogBackgroundService> _logger;

    public WcsApiLogBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WcsApiLogBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        
        // 创建无界通道，允许多个生产者和单个消费者
        // Create unbounded channel allowing multiple producers and single consumer
        _logChannel = Channel.CreateUnbounded<ApiCommunicationLog>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// 将日志加入队列（非阻塞，零等待）
    /// Enqueue log (non-blocking, zero wait)
    /// </summary>
    public void EnqueueLog(ApiCommunicationLog log)
    {
        // TryWrite 是非阻塞的，立即返回
        // TryWrite is non-blocking and returns immediately
        // 使用无界通道，永远不会失败
        // Using unbounded channel, will never fail
        _logChannel.Writer.TryWrite(log);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WCS API通信日志后台服务已启动");

        await foreach (var log in _logChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // 使用 Scoped 服务保存日志
                // Use Scoped service to save log
                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IApiCommunicationLogRepository>();
                
                await repository.SaveAsync(log, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存WCS API通信日志失败: ParcelId={ParcelId}", log.ParcelId);
                // 不抛出异常，继续处理下一条日志
            }
        }

        _logger.LogInformation("WCS API通信日志后台服务已停止");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止WCS API通信日志后台服务...");
        
        // 标记写入完成，等待所有日志处理完毕
        // Mark writing complete and wait for all logs to be processed
        _logChannel.Writer.Complete();
        
        await base.StopAsync(cancellationToken);
    }
}
