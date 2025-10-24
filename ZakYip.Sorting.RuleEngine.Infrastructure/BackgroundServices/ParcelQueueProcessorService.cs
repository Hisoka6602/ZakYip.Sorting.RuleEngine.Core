using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 包裹队列处理后台服务
/// Background service to process the parcel queue with FIFO ordering
/// </summary>
public class ParcelQueueProcessorService : BackgroundService
{
    private readonly ILogger<ParcelQueueProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ParcelQueueProcessorService(
        ILogger<ParcelQueueProcessorService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("包裹队列处理服务已启动");

        try
        {
            // 获取编排服务实例
            using var scope = _serviceProvider.CreateScope();
            var orchestrationService = scope.ServiceProvider.GetRequiredService<ParcelOrchestrationService>();

            // 开始处理队列
            await orchestrationService.ProcessQueueAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("包裹队列处理服务正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "包裹队列处理服务发生错误");
        }

        _logger.LogInformation("包裹队列处理服务已停止");
    }
}
