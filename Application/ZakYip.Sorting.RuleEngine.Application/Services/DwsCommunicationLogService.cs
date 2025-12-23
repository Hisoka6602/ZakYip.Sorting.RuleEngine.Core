using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// DWS通信日志服务 - 统一处理DWS通信日志的持久化
/// DWS communication log service - Unified handling of DWS communication log persistence
/// </summary>
/// <remarks>
/// 此服务消除了 DwsParcelBindingService 和 DwsDataReceivedEventHandler 中的重复代码（影分身）
/// This service eliminates duplicate code (shadow clone) in DwsParcelBindingService and DwsDataReceivedEventHandler
/// </remarks>
public class DwsCommunicationLogService
{
    private readonly IDwsCommunicationLogRepository _dwsCommunicationLogRepository;
    private readonly ISystemClock _clock;
    private readonly ILogger<DwsCommunicationLogService> _logger;

    public DwsCommunicationLogService(
        IDwsCommunicationLogRepository dwsCommunicationLogRepository,
        ISystemClock clock,
        ILogger<DwsCommunicationLogService> logger)
    {
        _dwsCommunicationLogRepository = dwsCommunicationLogRepository;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// 保存DWS通信日志到数据库（确保持久化）
    /// Save DWS communication log to database (ensure persistence)
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="sourceAddress">来源地址 / Source address</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    public async Task SaveAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new DwsCommunicationLog
            {
                CommunicationType = CommunicationType.Tcp,
                DwsAddress = sourceAddress ?? "未知DWS地址 / Unknown DWS Address",
                OriginalContent = JsonSerializer.Serialize(dwsData),
                FormattedContent = JsonSerializer.Serialize(dwsData, new JsonSerializerOptions { WriteIndented = true }),
                Barcode = dwsData.Barcode,
                Weight = dwsData.Weight,
                Volume = dwsData.Volume,
                ImagesJson = dwsData.Images != null && dwsData.Images.Any() 
                    ? JsonSerializer.Serialize(dwsData.Images) 
                    : null,
                CommunicationTime = _clock.LocalNow,
                IsSuccess = true,
                ErrorMessage = null
            };

            await _dwsCommunicationLogRepository.SaveAsync(log, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "DWS通信日志已保存: Barcode={Barcode}, Weight={Weight}g",
                dwsData.Barcode, dwsData.Weight);
        }
        catch (Exception ex)
        {
            // ⚠️ 持久化失败不应阻止DWS数据处理，仅记录错误
            // Persistence failure should not block DWS data processing, just log error
            _logger.LogError(ex,
                "❌ 保存DWS通信日志失败: Barcode={Barcode}",
                dwsData.Barcode);
        }
    }
}
