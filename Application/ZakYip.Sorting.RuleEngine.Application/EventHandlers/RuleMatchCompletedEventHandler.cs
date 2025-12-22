using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则匹配完成事件处理器
/// Rule match completed event handler
/// </summary>
public class RuleMatchCompletedEventHandler : INotificationHandler<RuleMatchCompletedEvent>
{
    private readonly ILogger<RuleMatchCompletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IDownstreamCommunication? _downstreamCommunication;
    private readonly IParcelInfoRepository _parcelRepository;
    private readonly ISystemClock _clock;

    public RuleMatchCompletedEventHandler(
        ILogger<RuleMatchCompletedEventHandler> logger,
        ILogRepository logRepository,
        IDownstreamCommunication? downstreamCommunication,
        IParcelInfoRepository parcelRepository,
        ISystemClock clock)
    {
        _logger = logger;
        _logRepository = logRepository;
        _downstreamCommunication = downstreamCommunication;
        _parcelRepository = parcelRepository;
        _clock = clock;
    }

    public async Task Handle(RuleMatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则匹配完成事件: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, CartCount={CartCount}",
            notification.ParcelId, notification.ChuteNumber, notification.CartCount);

        await _logRepository.LogInfoAsync(
            $"规则匹配已完成: {notification.ParcelId}",
            $"格口号: {notification.ChuteNumber}, 小车号: {notification.CartNumber}, 占用小车数: {notification.CartCount}").ConfigureAwait(false);

        // 更新包裹信息，标记为规则分拣模式 / Update parcel info, mark as rule-based sorting mode
        try
        {
            var parcel = await _parcelRepository.GetByIdAsync(notification.ParcelId, cancellationToken).ConfigureAwait(false);
            if (parcel != null)
            {
                parcel.TargetChute = notification.ChuteNumber;
                parcel.DecisionReason = "RuleEngine";
                parcel.SortingMode = SortingMode.RuleBased;  // 规则分拣模式
                parcel.LifecycleStage = ParcelLifecycleStage.ChuteAssigned;
                
                await _parcelRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "包裹已更新为规则分拣模式: ParcelId={ParcelId}, SortingMode={SortingMode}",
                    notification.ParcelId, SortingMode.RuleBased);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新包裹分拣模式失败: ParcelId={ParcelId}", notification.ParcelId);
        }

        // 发送格口号到下游分拣机系统
        // Send chute number to downstream sorter system
        try
        {
            if (_downstreamCommunication != null)
            {
                // 使用 TryParse 安全解析 ParcelId
                if (!long.TryParse(notification.ParcelId, out var parcelIdValue))
                {
                    _logger.LogWarning("解析 ParcelId 失败，输入值无效: {ParcelId}", notification.ParcelId);
                    await _logRepository.LogWarningAsync(
                        $"格口号发送失败: {notification.ParcelId}",
                        "ParcelId 格式无效").ConfigureAwait(false);
                }
                else if (!long.TryParse(notification.ChuteNumber, out var chuteIdValue))
                {
                    _logger.LogWarning("解析 ChuteNumber 失败，输入值无效: {ChuteNumber}", notification.ChuteNumber);
                    await _logRepository.LogWarningAsync(
                        $"格口号发送失败: {notification.ParcelId}",
                        "ChuteNumber 格式无效").ConfigureAwait(false);
                }
                else
                {
                    // 构造 ChuteAssignmentNotification 对象
                    var chuteNotification = new ChuteAssignmentNotification
                    {
                        ParcelId = parcelIdValue,
                        ChuteId = chuteIdValue,
                        AssignedAt = _clock.LocalNow
                    };

                    // 序列化为JSON
                    var json = JsonSerializer.Serialize(chuteNotification);

                    // 调用下游通信接口发送
                    await _downstreamCommunication.BroadcastChuteAssignmentAsync(json).ConfigureAwait(false);

                    _logger.LogInformation(
                        "格口号已发送到下游分拣机: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                        notification.ParcelId, notification.ChuteNumber);
                    await _logRepository.LogInfoAsync(
                        $"格口号已发送: {notification.ParcelId}",
                        $"格口号: {notification.ChuteNumber}").ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogWarning(
                    "下游通信未配置，无法发送格口号: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                    notification.ParcelId, notification.ChuteNumber);
                await _logRepository.LogWarningAsync(
                    $"格口号发送失败: {notification.ParcelId}",
                    "下游通信未配置").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "发送格口号到下游分拣机时发生异常: ParcelId={ParcelId}",
                notification.ParcelId);
            await _logRepository.LogErrorAsync(
                $"发送格口号异常: {notification.ParcelId}",
                ex.Message).ConfigureAwait(false);
        }
        
        // 关闭包裹处理空间（从缓存删除）
        // Close parcel processing space (remove from cache)
        // Note: This is handled by ParcelOrchestrationService
    }
}
