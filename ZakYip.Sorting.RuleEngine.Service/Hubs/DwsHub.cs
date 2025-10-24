using Microsoft.AspNetCore.SignalR;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

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
                Context.ConnectionAborted);

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
    /// 连接建立时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("DWS SignalR连接已建立: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
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
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// DWS数据接收结果
/// </summary>
public class DwsDataResult
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}
