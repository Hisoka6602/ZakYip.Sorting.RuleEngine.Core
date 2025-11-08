using System.Collections.Concurrent;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Application.Models;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹处理编排服务
/// </summary>
public class ParcelOrchestrationService
{
    private readonly ILogger<ParcelOrchestrationService> _logger;
    private readonly IPublisher _publisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly Channel<ParcelWorkItem> _parcelChannel;
    private readonly ConcurrentDictionary<string, ParcelProcessingContext> _processingContexts;
    private long _sequenceNumber;

    private readonly IParcelActivityTracker? _activityTracker;

    public ParcelOrchestrationService(
        ILogger<ParcelOrchestrationService> logger,
        IPublisher publisher,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        IParcelActivityTracker? activityTracker = null)
    {
        _logger = logger;
        _publisher = publisher;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _activityTracker = activityTracker;
        
        // 创建有界通道，确保FIFO处理
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _parcelChannel = Channel.CreateBounded<ParcelWorkItem>(channelOptions);
        _processingContexts = new ConcurrentDictionary<string, ParcelProcessingContext>();
        _sequenceNumber = 0;
    }

    /// <summary>
    /// 接收分拣程序信号，创建包裹处理空间
    /// </summary>
    public async Task<bool> CreateParcelAsync(string parcelId, string cartNumber, string? barcode = null, CancellationToken cancellationToken = default)
    {
        // 记录包裹创建活动（用于空闲检测）
        _activityTracker?.RecordParcelCreation();
        
        var sequence = Interlocked.Increment(ref _sequenceNumber);
        
        // 在缓存中开辟空间
        var context = new ParcelProcessingContext
        {
            ParcelId = parcelId,
            CartNumber = cartNumber,
            Barcode = barcode,
            SequenceNumber = sequence,
            CreatedAt = DateTime.Now
        };
        
        if (!_processingContexts.TryAdd(parcelId, context))
        {
            _logger.LogWarning("包裹ID已存在: {ParcelId}", parcelId);
            return false;
        }

        // 将包裹加入FIFO队列
        var workItem = new ParcelWorkItem
        {
            ParcelId = parcelId,
            SequenceNumber = sequence,
            WorkType = WorkItemType.Create
        };
        
        await _parcelChannel.Writer.WriteAsync(workItem, cancellationToken);
        
        _logger.LogInformation("包裹已加入处理队列: {ParcelId}, Sequence={Sequence}", parcelId, sequence);
        return true;
    }

    /// <summary>
    /// 接收DWS数据
    /// </summary>
    public async Task<bool> ReceiveDwsDataAsync(string parcelId, DwsData dwsData, CancellationToken cancellationToken = default)
    {
        if (!_processingContexts.TryGetValue(parcelId, out var context))
        {
            _logger.LogWarning("包裹不存在或已关闭: {ParcelId}", parcelId);
            return false;
        }

        context.DwsData = dwsData;
        context.DwsReceivedAt = DateTime.Now;
        
        // 将DWS处理加入队列
        var workItem = new ParcelWorkItem
        {
            ParcelId = parcelId,
            SequenceNumber = context.SequenceNumber,
            WorkType = WorkItemType.ProcessDws
        };
        
        await _parcelChannel.Writer.WriteAsync(workItem, cancellationToken);
        
        _logger.LogInformation("DWS数据已加入处理队列: {ParcelId}", parcelId);
        return true;
    }

    /// <summary>
    /// 处理队列中的工作项（FIFO）
    /// </summary>
    public async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        await foreach (var workItem in _parcelChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessWorkItemAsync(workItem, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理工作项失败: ParcelId={ParcelId}, WorkType={WorkType}",
                    workItem.ParcelId, workItem.WorkType);
            }
        }
    }

    private async Task ProcessWorkItemAsync(ParcelWorkItem workItem, CancellationToken cancellationToken)
    {
        if (!_processingContexts.TryGetValue(workItem.ParcelId, out var context))
        {
            _logger.LogWarning("包裹上下文不存在: {ParcelId}", workItem.ParcelId);
            return;
        }

        switch (workItem.WorkType)
        {
            case WorkItemType.Create:
                // 发布包裹创建事件
                await _publisher.Publish(new ParcelCreatedEvent
                {
                    ParcelId = context.ParcelId,
                    CartNumber = context.CartNumber,
                    Barcode = context.Barcode,
                    SequenceNumber = context.SequenceNumber
                }, cancellationToken);
                break;

            case WorkItemType.ProcessDws:
                {
                    if (context.DwsData == null)
                    {
                        _logger.LogWarning("DWS数据为空: {ParcelId}", workItem.ParcelId);
                        return;
                    }

                    // 发布DWS数据接收事件
                    await _publisher.Publish(new DwsDataReceivedEvent
                    {
                        ParcelId = context.ParcelId,
                        DwsData = context.DwsData
                    }, cancellationToken);

                    // 等待WCS API响应后，执行规则匹配
                    await Task.Delay(100, cancellationToken); // 简单延迟，实际应该等待事件完成
                    
                    // 执行规则匹配
                    var parcelInfo = new ParcelInfo
                    {
                        ParcelId = context.ParcelId,
                        CartNumber = context.CartNumber,
                        Barcode = context.Barcode ?? context.DwsData.Barcode,
                        Status = ParcelStatus.Processing
                    };

                    // 创建作用域以访问 Scoped 服务
                    using var scope = _serviceProvider.CreateScope();
                    var ruleEngineService = scope.ServiceProvider.GetRequiredService<IRuleEngineService>();
                    
                    var chuteNumber = await ruleEngineService.EvaluateRulesAsync(
                        parcelInfo,
                        context.DwsData,
                        context.WcsApiResponse,
                        cancellationToken);

                    if (chuteNumber != null)
                    {
                        // 发布规则匹配完成事件
                        await _publisher.Publish(new RuleMatchCompletedEvent
                        {
                            ParcelId = context.ParcelId,
                            ChuteNumber = chuteNumber,
                            CartNumber = context.CartNumber,
                            CartCount = CalculateCartCount(context.DwsData)
                        }, cancellationToken);

                        // 关闭处理空间（从缓存删除）
                        _processingContexts.TryRemove(context.ParcelId, out _);
                        _logger.LogInformation("包裹处理完成并已清理: {ParcelId}", context.ParcelId);
                    }
                    break;
                }
        }
    }

    private int CalculateCartCount(DwsData dwsData)
    {
        // 根据体积或重量计算占用小车数
        // 简单示例：大于100000立方厘米占用2个小车
        return dwsData.Volume > 100000 ? 2 : 1;
    }
}
