using System.Collections.Concurrent;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly ISystemClock _clock;
    private readonly IDwsTimeoutSettings _timeoutSettings;
    private long _sequenceNumber;

    private readonly IParcelActivityTracker? _activityTracker;

    public ParcelOrchestrationService(
        ILogger<ParcelOrchestrationService> logger,
        IPublisher publisher,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ISystemClock clock,
        IDwsTimeoutSettings timeoutSettings,
        IParcelActivityTracker? activityTracker = null)
    {
        _logger = logger;
        _publisher = publisher;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _clock = clock;
        _timeoutSettings = timeoutSettings;
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
            CreatedAt = _clock.LocalNow
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
        
        await _parcelChannel.Writer.WriteAsync(workItem, cancellationToken).ConfigureAwait(false);
        
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

        // 检查是否启用超时检查
        if (_timeoutSettings.Enabled)
        {
            var elapsedSeconds = (_clock.LocalNow - context.CreatedAt).TotalSeconds;
            
            // 检查是否太早接收（可能是上一个包裹的DWS数据）
            if (elapsedSeconds < _timeoutSettings.MinDwsWaitSeconds)
            {
                _logger.LogWarning(
                    "DWS数据接收过早，可能是上一个包裹的数据: ParcelId={ParcelId}, ElapsedSeconds={ElapsedSeconds:F2}, MinWaitSeconds={MinWaitSeconds}",
                    parcelId, elapsedSeconds, _timeoutSettings.MinDwsWaitSeconds);
                return false;
            }
            
            // 检查是否超时
            if (elapsedSeconds > _timeoutSettings.MaxDwsWaitSeconds)
            {
                _logger.LogWarning(
                    "DWS数据接收超时，拒绝接收: ParcelId={ParcelId}, ElapsedSeconds={ElapsedSeconds:F2}, MaxWaitSeconds={MaxWaitSeconds}",
                    parcelId, elapsedSeconds, _timeoutSettings.MaxDwsWaitSeconds);
                return false;
            }
        }

        context.DwsData = dwsData;
        context.DwsReceivedAt = _clock.LocalNow;
        
        // 将DWS处理加入队列
        var workItem = new ParcelWorkItem
        {
            ParcelId = parcelId,
            SequenceNumber = context.SequenceNumber,
            WorkType = WorkItemType.ProcessDws
        };
        
        await _parcelChannel.Writer.WriteAsync(workItem, cancellationToken).ConfigureAwait(false);
        
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
                await ProcessWorkItemAsync(workItem, cancellationToken).ConfigureAwait(false);
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
                        cancellationToken).ConfigureAwait(false);

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
                
            case WorkItemType.ProcessTimeout:
                {
                    // 处理超时包裹，分配到异常格口
                    _logger.LogWarning(
                        "处理超时包裹: ParcelId={ParcelId}, CreatedAt={CreatedAt}, ElapsedSeconds={ElapsedSeconds:F2}, ExceptionChuteId={ExceptionChuteId}",
                        context.ParcelId, context.CreatedAt, (_clock.LocalNow - context.CreatedAt).TotalSeconds, 
                        _timeoutSettings.ExceptionChuteId);
                    
                    // 分配到异常格口
                    var exceptionChuteNumber = _timeoutSettings.ExceptionChuteId.ToString();
                    
                    // 发布规则匹配完成事件（使用异常格口）
                    await _publisher.Publish(new RuleMatchCompletedEvent
                    {
                        ParcelId = context.ParcelId,
                        ChuteNumber = exceptionChuteNumber,
                        CartNumber = context.CartNumber,
                        CartCount = 1 // 超时包裹默认占用1个小车
                    }, cancellationToken);
                    
                    // 清理处理上下文
                    _processingContexts.TryRemove(context.ParcelId, out _);
                    _logger.LogInformation("超时包裹已处理并分配到异常格口: ParcelId={ParcelId}, ExceptionChute={ExceptionChute}", 
                        context.ParcelId, exceptionChuteNumber);
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
    
    /// <summary>
    /// 检查并处理超时包裹
    /// Check and process timed-out parcels
    /// </summary>
    public async Task CheckTimeoutParcelsAsync(CancellationToken cancellationToken = default)
    {
        if (!_timeoutSettings.Enabled)
        {
            return;
        }
        
        var now = _clock.LocalNow;
        var timedOutParcels = new List<string>();
        
        // 查找所有超时的包裹（还没有接收到DWS数据）
        foreach (var kvp in _processingContexts)
        {
            var context = kvp.Value;
            
            // 如果已经接收了DWS数据，跳过
            if (context.DwsReceivedAt.HasValue)
            {
                continue;
            }
            
            var elapsedSeconds = (now - context.CreatedAt).TotalSeconds;
            
            // 检查是否超时
            if (elapsedSeconds > _timeoutSettings.MaxDwsWaitSeconds)
            {
                timedOutParcels.Add(kvp.Key);
            }
        }
        
        // 处理超时包裹
        foreach (var parcelId in timedOutParcels)
        {
            if (_processingContexts.TryGetValue(parcelId, out var context))
            {
                _logger.LogWarning(
                    "检测到超时包裹: ParcelId={ParcelId}, CreatedAt={CreatedAt}, ElapsedSeconds={ElapsedSeconds:F2}",
                    parcelId, context.CreatedAt, (now - context.CreatedAt).TotalSeconds);
                
                // 将超时处理加入队列
                var workItem = new ParcelWorkItem
                {
                    ParcelId = parcelId,
                    SequenceNumber = context.SequenceNumber,
                    WorkType = WorkItemType.ProcessTimeout
                };
                
                await _parcelChannel.Writer.WriteAsync(workItem, cancellationToken).ConfigureAwait(false);
            }
        }
        
        if (timedOutParcels.Count > 0)
        {
            _logger.LogInformation("共检测到 {Count} 个超时包裹", timedOutParcels.Count);
        }
    }
}
