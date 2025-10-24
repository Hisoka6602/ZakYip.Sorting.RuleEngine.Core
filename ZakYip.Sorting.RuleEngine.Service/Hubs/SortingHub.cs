using Microsoft.AspNetCore.SignalR;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Service.Hubs;

/// <summary>
/// 分拣机实时通信Hub
/// </summary>
public class SortingHub : Hub
{
    private readonly ParcelOrchestrationService _orchestrationService;
    private readonly ILogger<SortingHub> _logger;

    public SortingHub(
        ParcelOrchestrationService orchestrationService,
        ILogger<SortingHub> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// 接收分拣程序信号，创建包裹处理空间
    /// </summary>
    public async Task<ParcelCreationResult> CreateParcel(string parcelId, string cartNumber, string? barcode)
    {
        try
        {
            _logger.LogInformation(
                "SignalR收到分拣机信号 - ParcelId: {ParcelId}, CartNumber: {CartNumber}, ConnectionId: {ConnectionId}",
                parcelId, cartNumber, Context.ConnectionId);

            var success = await _orchestrationService.CreateParcelAsync(
                parcelId,
                cartNumber,
                barcode,
                Context.ConnectionAborted);

            if (success)
            {
                return new ParcelCreationResult
                {
                    Success = true,
                    ParcelId = parcelId,
                    Message = "包裹处理空间已创建，等待DWS数据"
                };
            }
            else
            {
                return new ParcelCreationResult
                {
                    Success = false,
                    ParcelId = parcelId,
                    Message = "包裹ID已存在或创建失败"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR创建包裹处理空间失败: {ParcelId}", parcelId);
            return new ParcelCreationResult
            {
                Success = false,
                ParcelId = parcelId,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 发送格口号到分拣机
    /// </summary>
    public async Task SendChuteNumber(string parcelId, string chuteNumber, string cartNumber, int cartCount)
    {
        try
        {
            _logger.LogInformation(
                "SignalR发送格口号 - ParcelId: {ParcelId}, Chute: {Chute}, ConnectionId: {ConnectionId}",
                parcelId, chuteNumber, Context.ConnectionId);

            await Clients.Caller.SendAsync("ReceiveChuteNumber", parcelId, chuteNumber, cartNumber, cartCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR发送格口号失败: {ParcelId}", parcelId);
        }
    }

    /// <summary>
    /// 连接建立时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("分拣机SignalR连接已建立: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 连接断开时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "分拣机SignalR连接异常断开: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("分拣机SignalR连接已断开: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// 包裹创建结果
/// </summary>
public class ParcelCreationResult
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}
