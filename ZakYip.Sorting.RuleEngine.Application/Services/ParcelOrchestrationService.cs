using System.Collections.Concurrent;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹处理编排服务
/// Parcel processing orchestration service with FIFO ordering and concurrent processing
/// </summary>
public class ParcelOrchestrationService
{
    private readonly ILogger<ParcelOrchestrationService> _logger;
    private readonly IPublisher _publisher;
    private readonly IRuleEngineService _ruleEngineService;
    private readonly IMemoryCache _cache;
    private readonly Channel<ParcelWorkItem> _parcelChannel;
    private readonly ConcurrentDictionary<string, ParcelProcessingContext> _processingContexts;
    private long _sequenceNumber;

    private readonly IParcelActivityTracker? _activityTracker;

    public ParcelOrchestrationService(
        ILogger<ParcelOrchestrationService> logger,
        IPublisher publisher,
        IRuleEngineService ruleEngineService,
        IMemoryCache cache,
        IParcelActivityTracker? activityTracker = null)
    {
        _logger = logger;
        _publisher = publisher;
        _ruleEngineService = ruleEngineService;
        _cache = cache;
        _activityTracker = activityTracker;
        
        // 创建有界通道，确保FIFO处理
        // Create bounded channel for FIFO processing
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
    /// Receive sorting machine signal and create parcel processing space
    /// </summary>
    public async Task<bool> CreateParcelAsync(string parcelId, string cartNumber, string? barcode = null, CancellationToken cancellationToken = default)
    {
        // 记录包裹创建活动（用于空闲检测）
        _activityTracker?.RecordParcelCreation();
        
        var sequence = Interlocked.Increment(ref _sequenceNumber);
        
        // 在缓存中开辟空间
        // Allocate space in cache
        var context = new ParcelProcessingContext
        {
            ParcelId = parcelId,
            CartNumber = cartNumber,
            Barcode = barcode,
            SequenceNumber = sequence,
            CreatedAt = DateTime.UtcNow
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
    /// Receive DWS data for a parcel
    /// </summary>
    public async Task<bool> ReceiveDwsDataAsync(string parcelId, DwsData dwsData, CancellationToken cancellationToken = default)
    {
        if (!_processingContexts.TryGetValue(parcelId, out var context))
        {
            _logger.LogWarning("包裹不存在或已关闭: {ParcelId}", parcelId);
            return false;
        }

        context.DwsData = dwsData;
        context.DwsReceivedAt = DateTime.UtcNow;
        
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
    /// 处理队列中的工作项
    /// Process work items from the queue (FIFO)
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

                // 等待第三方API响应后，执行规则匹配
                await Task.Delay(100, cancellationToken); // 简单延迟，实际应该等待事件完成
                
                // 执行规则匹配
                var parcelInfo = new ParcelInfo
                {
                    ParcelId = context.ParcelId,
                    CartNumber = context.CartNumber,
                    Barcode = context.Barcode ?? context.DwsData.Barcode,
                    Status = ParcelStatus.Processing
                };

                var chuteNumber = await _ruleEngineService.EvaluateRulesAsync(
                    parcelInfo,
                    context.DwsData,
                    context.ThirdPartyResponse,
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

    private int CalculateCartCount(DwsData dwsData)
    {
        // 根据体积或重量计算占用小车数
        // Calculate cart count based on volume or weight
        // 简单示例：大于100000立方厘米占用2个小车
        return dwsData.Volume > 100000 ? 2 : 1;
    }
}

/// <summary>
/// 包裹处理上下文
/// Parcel processing context stored in cache
/// </summary>
public class ParcelProcessingContext
{
    public required string ParcelId { get; init; }
    public required string CartNumber { get; init; }
    public string? Barcode { get; init; }
    public long SequenceNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DwsReceivedAt { get; set; }
    public DwsData? DwsData { get; set; }
    public ThirdPartyResponse? ThirdPartyResponse { get; set; }
}

/// <summary>
/// 工作项
/// Work item for the processing queue
/// </summary>
public class ParcelWorkItem
{
    public required string ParcelId { get; init; }
    public long SequenceNumber { get; init; }
    public required WorkItemType WorkType { get; init; }
}

/// <summary>
/// 工作项类型
/// Work item type
/// </summary>
public enum WorkItemType
{
    Create,
    ProcessDws
}
