using Microsoft.AspNetCore.SignalR;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using System.Reflection;

namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

/// <summary>
/// DWS实时通信Hub
/// </summary>
public class DwsHub : Hub
{
    private readonly ParcelOrchestrationService _orchestrationService;
    private readonly ILogger<DwsHub> _logger;

    public DwsHub(
        ParcelOrchestrationService orchestrationService,
        ILogger<DwsHub> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// 接收DWS数据
    /// </summary>
    public async Task<DwsDataResult> ReceiveDwsData(
        string parcelId,
        string? barcode,
        decimal weight,
        decimal length,
        decimal width,
        decimal height,
        decimal volume)
    {
        try
        {
            _logger.LogInformation(
                "SignalR收到DWS数据 - ParcelId: {ParcelId}, Weight: {Weight}g, ConnectionId: {ConnectionId}",
                parcelId, weight, Context.ConnectionId);

            var dwsData = new DwsData
            {
                Barcode = barcode ?? string.Empty,
                Weight = weight,
                Length = length,
                Width = width,
                Height = height,
                Volume = volume
            };

            var success = await _orchestrationService.ReceiveDwsDataAsync(
                parcelId,
                dwsData,
                Context.ConnectionAborted).ConfigureAwait(false);

            if (success)
            {
                return new DwsDataResult
                {
                    Success = true,
                    ParcelId = parcelId,
                    Message = "DWS数据已接收，开始处理"
                };
            }
            else
            {
                return new DwsDataResult
                {
                    Success = false,
                    ParcelId = parcelId,
                    Message = "包裹不存在或已关闭"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR接收DWS数据失败: {ParcelId}", parcelId);
            return new DwsDataResult
            {
                Success = false,
                ParcelId = parcelId,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取系统版本信息
    /// </summary>
    public Task<object> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        return Task.FromResult<object>(new
        {
            version = version?.ToString() ?? "1.7.0",
            productName = "ZakYip 分拣规则引擎",
            description = "DWS通信Hub"
        });
    }

    /// <summary>
    /// 连接建立时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("DWS SignalR连接已建立: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 连接断开时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "DWS SignalR连接异常断开: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("DWS SignalR连接已断开: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
