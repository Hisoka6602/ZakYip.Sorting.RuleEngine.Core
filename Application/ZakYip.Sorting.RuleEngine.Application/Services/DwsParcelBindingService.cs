using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// DWS包裹绑定服务 - 处理DWS数据接收并绑定到包裹
/// DWS parcel binding service - handles DWS data reception and binds to parcels
/// </summary>
/// <remarks>
/// 此服务负责：
/// 1. 持久化DWS通信日志
/// 2. 绑定DWS数据到包裹（ParcelId为空时自动查找）
/// 3. 发布DwsDataReceivedEvent给MediatR，触发完整业务流程（规则引擎+WCS+格口分配）
/// 
/// This service is responsible for:
/// 1. Persisting DWS communication log
/// 2. Binding DWS data to parcel (auto-find when ParcelId is empty)
/// 3. Publishing DwsDataReceivedEvent to MediatR to trigger complete business flow (rule engine + WCS + chute assignment)
/// </remarks>
public class DwsParcelBindingService
{
    private readonly ILogger<DwsParcelBindingService> _logger;
    private readonly IPublisher _publisher;
    private readonly ILogRepository _logRepository;
    private readonly ISystemClock _clock;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly DwsCommunicationLogService _dwsCommunicationLogService;

    public DwsParcelBindingService(
        ILogger<DwsParcelBindingService> logger,
        IPublisher publisher,
        ILogRepository logRepository,
        ISystemClock clock,
        IParcelInfoRepository parcelInfoRepository,
        ParcelCacheService cacheService,
        DwsCommunicationLogService dwsCommunicationLogService)
    {
        _logger = logger;
        _publisher = publisher;
        _logRepository = logRepository;
        _clock = clock;
        _parcelInfoRepository = parcelInfoRepository;
        _cacheService = cacheService;
        _dwsCommunicationLogService = dwsCommunicationLogService;
    }

    /// <summary>
    /// 处理DWS数据接收事件，绑定到包裹并触发完整业务流程
    /// Handle DWS data reception event, bind to parcel and trigger complete business flow
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="sourceAddress">来源地址 / Source address</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    public async Task HandleDwsDataAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "📦 [步骤1-DWS接收] 处理DWS数据: ParcelId={ParcelId}, Barcode={Barcode}, Weight={Weight}g",
                dwsData.ParcelId, dwsData.Barcode, dwsData.Weight);

            // ✅ 持久化DWS通信日志（并行执行，不阻塞关键业务路径）
            // Persist DWS communication log (parallel execution, don't block critical business path)
            var logTask = Task.Run(async () =>
            {
                await _dwsCommunicationLogService.SaveAsync(dwsData, sourceAddress, cancellationToken)
                    .ConfigureAwait(false);
            }, cancellationToken);

            // 🔍 智能包裹绑定：ParcelId为空时自动查找最新未绑定包裹
            // Smart parcel binding: auto-find latest unbound parcel when ParcelId is empty
            string? parcelId = await FindOrBindParcelIdAsync(dwsData, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(parcelId))
            {
                _logger.LogWarning(
                    "⚠️ [步骤1-DWS接收] 无法绑定DWS数据，未找到待绑定包裹: Barcode={Barcode}",
                    dwsData.Barcode);
                await _logRepository.LogWarningAsync(
                    $"DWS数据无法绑定: Barcode={dwsData.Barcode}",
                    "未找到待绑定的包裹。包裹必须由下游分拣机预先创建。").ConfigureAwait(false);
                
                // 等待日志任务完成
                await logTask.ConfigureAwait(false);
                return;
            }

            _logger.LogInformation(
                "🔗 [步骤2-包裹绑定] DWS数据已绑定到包裹 / DWS data bound to parcel: ParcelId={ParcelId}, Barcode={Barcode}",
                parcelId, dwsData.Barcode);

            // ✅ 发布 DwsDataReceivedEvent 给 MediatR，触发完整业务流程：
            // - DwsDataReceivedEventHandler: 更新包裹信息 → 规则引擎匹配 → WCS请求格口 → 发送格口给分拣机
            // Publish DwsDataReceivedEvent to MediatR to trigger complete business flow:
            // - DwsDataReceivedEventHandler: Update parcel info → Rule engine matching → WCS request chute → Send chute to sorter
            await _publisher.Publish(new DwsDataReceivedEvent
            {
                ParcelId = parcelId,
                DwsData = dwsData,
                ReceivedAt = _clock.LocalNow,
                SourceAddress = sourceAddress
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "📢 [步骤2-事件发布] DwsDataReceivedEvent 已发布 / Event published: ParcelId={ParcelId}",
                parcelId);
            
            // 等待日志任务完成（不阻塞事件发布）
            await logTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [DWS数据处理] 异常 / Exception: Barcode={Barcode}",
                dwsData.Barcode);
            
            // ⚠️ 即使发生异常，也记录警告日志，确保异常不影响其他DWS数据处理
            // Even if exception occurs, log warning to ensure it doesn't affect other DWS data processing
            try
            {
                await _logRepository.LogErrorAsync(
                    $"DWS数据处理异常: Barcode={dwsData.Barcode}",
                    ex.Message).ConfigureAwait(false);
            }
            catch
            {
                // 忽略日志记录失败 / Ignore log recording failure
            }
        }
    }

    /// <summary>
    /// 查找或绑定包裹ID（从缓存或数据库）
    /// Find or bind parcel ID (from cache or database)
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹ID，如果未找到则返回null / Parcel ID, or null if not found</returns>
    private async Task<string?> FindOrBindParcelIdAsync(DwsData dwsData, CancellationToken cancellationToken)
    {
        try
        {
            // 场景1: DWS数据中包含ParcelId，直接使用
            // Scenario 1: DWS data contains ParcelId, use it directly
            if (!string.IsNullOrEmpty(dwsData.ParcelId))
            {
                _logger.LogDebug(
                    "DWS数据包含ParcelId: {ParcelId}",
                    dwsData.ParcelId);
                return dwsData.ParcelId;
            }

            // 场景2: DWS数据中没有ParcelId（常见情况），从缓存中查找最新未绑定Barcode的包裹
            // Scenario 2: DWS data doesn't contain ParcelId (common case), find latest parcel without Barcode from cache
            _logger.LogInformation(
                "DWS数据不包含ParcelId，尝试从缓存查找最新未绑定包裹: Barcode={Barcode}",
                dwsData.Barcode);

            // 优先从缓存查找（性能更好，避免数据库查询）
            // First try cache (better performance, avoids database query)
            var latestParcel = await GetLatestUnboundParcelAsync(cancellationToken).ConfigureAwait(false);
            
            if (latestParcel != null)
            {
                _logger.LogInformation(
                    "✅ 从缓存找到最新未绑定包裹: ParcelId={ParcelId}，将绑定到 Barcode={Barcode}",
                    latestParcel.ParcelId, dwsData.Barcode);
                return latestParcel.ParcelId;
            }

            // 如果缓存中没有，尝试从数据库查找（降级方案）
            // If not in cache, try database as fallback
            _logger.LogDebug(
                "缓存中未找到未绑定包裹，尝试从数据库查找: Barcode={Barcode}",
                dwsData.Barcode);

            latestParcel = await _parcelInfoRepository
                .GetLatestWithoutDwsDataAsync(cancellationToken)
                .ConfigureAwait(false);

            if (latestParcel != null)
            {
                _logger.LogInformation(
                    "✅ 从数据库找到最新未绑定包裹: ParcelId={ParcelId}，将绑定到 Barcode={Barcode}",
                    latestParcel.ParcelId, dwsData.Barcode);
                return latestParcel.ParcelId;
            }

            // 场景3: 未找到现有包裹，自动创建新包裹（使用条码或时间戳作为ParcelId）
            // Scenario 3: No existing parcel found, auto-create new parcel (use Barcode or timestamp as ParcelId)
            _logger.LogInformation(
                "🆕 缓存和数据库均未找到未绑定的包裹，将自动创建新包裹: Barcode={Barcode}",
                dwsData.Barcode);
            
            // 生成ParcelId：优先使用Barcode，如果为空则使用时间戳
            // Generate ParcelId: prefer Barcode, fallback to timestamp if empty
            var newParcelId = !string.IsNullOrEmpty(dwsData.Barcode) 
                ? dwsData.Barcode 
                : _clock.LocalNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            
            // 创建新包裹
            // Create new parcel
            var newParcel = new ParcelInfo
            {
                ParcelId = newParcelId,
                Barcode = dwsData.Barcode,
                LifecycleStage = Domain.Enums.ParcelLifecycleStage.Created,
                CreatedAt = _clock.LocalNow
            };
            
            try
            {
                // 保存到数据库
                // Save to database
                await _parcelInfoRepository.AddAsync(newParcel, cancellationToken).ConfigureAwait(false);
                
                // 添加到缓存
                // Add to cache
                await _cacheService.SetAsync(newParcel, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "✅ 新包裹已创建: ParcelId={ParcelId}, Barcode={Barcode}",
                    newParcelId, dwsData.Barcode);
                
                // 记录创建日志
                // Log creation
                await _logRepository.LogInfoAsync(
                    $"自动创建包裹: ParcelId={newParcelId}",
                    $"DWS数据到达但未找到预先创建的包裹，已自动创建。Barcode={dwsData.Barcode}").ConfigureAwait(false);
                
                return newParcelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ 创建新包裹失败: Barcode={Barcode}",
                    dwsData.Barcode);
                
                await _logRepository.LogErrorAsync(
                    $"创建包裹失败: Barcode={dwsData.Barcode}",
                    ex.Message).ConfigureAwait(false);
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 查找包裹ID失败: Barcode={Barcode}",
                dwsData.Barcode);
            
            // 数据库异常不应阻止DWS数据接收，返回null并记录警告
            // Database exception should not block DWS data reception, return null and log warning
            return null;
        }
    }

    /// <summary>
    /// 获取最新的未绑定包裹（优先缓存，降级数据库）
    /// Get the latest unbound parcel (cache first, database fallback)
    /// </summary>
    /// <remarks>
    /// 注意：由于 IMemoryCache 不支持条件查询，当前实现从数据库查询后尝试从缓存加载
    /// Note: Since IMemoryCache doesn't support conditional queries, current implementation queries database then tries cache
    /// </remarks>
    private async Task<ParcelInfo?> GetLatestUnboundParcelAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 从数据库查询最新未绑定包裹的ID
            var latestParcel = await _parcelInfoRepository
                .GetLatestWithoutDwsDataAsync(cancellationToken)
                .ConfigureAwait(false);

            if (latestParcel == null)
            {
                return null;
            }

            // 尝试从缓存加载完整的包裹信息（可能有更新的数据）
            // Try to load full parcel info from cache (may have more up-to-date data)
            var cachedParcel = await _cacheService.GetAsync(latestParcel.ParcelId, cancellationToken)
                .ConfigureAwait(false);

            return cachedParcel ?? latestParcel;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "从缓存获取最新包裹失败，将使用降级方案");
            return null;
        }
    }
}
