using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using ZakYip.Sorting.RuleEngine.Application.DTOs;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹处理服务实现
/// </summary>
public class ParcelProcessingService : IParcelProcessingService
{
    private readonly IRuleEngineService _ruleEngineService;
    private readonly IWcsApiAdapterFactory _apiAdapterFactory;
    private readonly ILogRepository _logRepository;
    private readonly ILogger<ParcelProcessingService> _logger;
    private readonly ObjectPool<Stopwatch> _stopwatchPool;

    public ParcelProcessingService(
        IRuleEngineService ruleEngineService,
        IWcsApiAdapterFactory apiAdapterFactory,
        ILogRepository logRepository,
        ILogger<ParcelProcessingService> logger)
    {
        _ruleEngineService = ruleEngineService;
        _apiAdapterFactory = apiAdapterFactory;
        _logRepository = logRepository;
        _logger = logger;
        
        // 创建Stopwatch对象池以提高性能
        var policy = new DefaultPooledObjectPolicy<Stopwatch>();
        _stopwatchPool = new DefaultObjectPool<Stopwatch>(policy, ObjectPoolDefaults.StopwatchPoolSize);
    }

    /// <summary>
    /// 处理单个包裹
    /// </summary>
    public async Task<ParcelProcessResponse> ProcessParcelAsync(
        ParcelProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = _stopwatchPool.Get();
        stopwatch.Restart();

        try
        {
            _logger.LogInformation("开始处理包裹: {ParcelId}", request.ParcelId);

            // 创建包裹实体
            var parcelInfo = new ParcelInfo
            {
                ParcelId = request.ParcelId,
                CartNumber = request.CartNumber,
                Barcode = request.Barcode,
                Status = ParcelStatus.Processing
            };

            // 创建DWS数据（如果提供）
            DwsData? dwsData = null;
            if (request.Weight.HasValue || request.Volume.HasValue)
            {
                dwsData = new DwsData
                {
                    Barcode = request.Barcode ?? string.Empty,
                    Weight = request.Weight ?? 0,
                    Length = request.Length ?? 0,
                    Width = request.Width ?? 0,
                    Height = request.Height ?? 0,
                    Volume = request.Volume ?? 0
                };
            }

            // 调用WCS API获取响应（如果条码可用且DWS数据存在）
            WcsApiResponse? thirdPartyResponse = null;
            if (!string.IsNullOrEmpty(request.Barcode) && dwsData != null)
            {
                try
                {
                    thirdPartyResponse = await _apiAdapterFactory.GetActiveAdapter().RequestChuteAsync(
                        request.ParcelId,
                        dwsData,
                        null, // 此上下文中OCR数据不可用
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WCS API调用失败，继续使用规则引擎: {ParcelId}", request.ParcelId);
                    await _logRepository.LogWarningAsync(
                        $"WCS API调用失败: {request.ParcelId}",
                        ex.ToString());
                }
            }

            // 使用规则引擎计算格口号
            var chuteNumber = await _ruleEngineService.EvaluateRulesAsync(
                parcelInfo, dwsData, thirdPartyResponse, cancellationToken);

            // 更新包裹状态
            parcelInfo.ChuteNumber = chuteNumber;
            parcelInfo.Status = chuteNumber != null ? ParcelStatus.Completed : ParcelStatus.Failed;
            parcelInfo.UpdatedAt = DateTime.Now;

            stopwatch.Stop();

            _logger.LogInformation(
                "包裹处理完成: {ParcelId}, 格口号: {ChuteNumber}, 耗时: {ElapsedMs}ms",
                request.ParcelId, chuteNumber, stopwatch.ElapsedMilliseconds);

            await _logRepository.LogInfoAsync(
                $"包裹处理成功: {request.ParcelId}",
                $"格口号: {chuteNumber}, 耗时: {stopwatch.ElapsedMilliseconds}ms");

            return new ParcelProcessResponse
            {
                Success = chuteNumber != null,
                ParcelId = request.ParcelId,
                ChuteNumber = chuteNumber,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "处理包裹失败: {ParcelId}", request.ParcelId);

            await _logRepository.LogErrorAsync(
                $"包裹处理失败: {request.ParcelId}",
                ex.ToString());

            return new ParcelProcessResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        finally
        {
            stopwatch.Reset();
            _stopwatchPool.Return(stopwatch);
        }
    }

    /// <summary>
    /// 批量处理包裹，使用并行执行提高性能
    /// 注意：对于ParcelProcessResponse，使用Task.WhenAll并行处理提供了良好的性能
    /// </summary>
    public async Task<IEnumerable<ParcelProcessResponse>> ProcessParcelsAsync(
        IEnumerable<ParcelProcessRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        var count = requestList.Count;
        
        _logger.LogInformation("开始批量处理 {Count} 个包裹", count);

        // 并行处理以提高性能，使用Task.WhenAll在此场景中安全且高效
        var tasks = requestList.Select(request => 
            ProcessParcelAsync(request, cancellationToken));

        var responses = await Task.WhenAll(tasks);

        _logger.LogInformation("批量处理完成，成功: {Success}，失败: {Failed}",
            responses.Count(r => r.Success),
            responses.Count(r => !r.Success));

        return responses;
    }
}
