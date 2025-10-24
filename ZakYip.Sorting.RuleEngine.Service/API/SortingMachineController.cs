using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 分拣机信号接收API控制器
/// Sorting machine signal receiving API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SortingMachineController : ControllerBase
{
    private readonly ParcelOrchestrationService _orchestrationService;
    private readonly ILogger<SortingMachineController> _logger;

    public SortingMachineController(
        ParcelOrchestrationService orchestrationService,
        ILogger<SortingMachineController> logger)
    {
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    /// <summary>
    /// 接收分拣程序信号，创建包裹处理空间
    /// Receive sorting machine signal to create parcel processing space
    /// </summary>
    /// <param name="request">包裹创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    [HttpPost("create-parcel")]
    public async Task<ActionResult<ParcelCreationResponse>> CreateParcel(
        [FromBody] ParcelCreationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "收到分拣机信号 - ParcelId: {ParcelId}, CartNumber: {CartNumber}",
                request.ParcelId, request.CartNumber);

            var success = await _orchestrationService.CreateParcelAsync(
                request.ParcelId,
                request.CartNumber,
                request.Barcode,
                cancellationToken);

            if (success)
            {
                return Ok(new ParcelCreationResponse
                {
                    Success = true,
                    ParcelId = request.ParcelId,
                    Message = "包裹处理空间已创建，等待DWS数据"
                });
            }
            else
            {
                return BadRequest(new ParcelCreationResponse
                {
                    Success = false,
                    ParcelId = request.ParcelId,
                    Message = "包裹ID已存在或创建失败"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建包裹处理空间失败: {ParcelId}", request.ParcelId);
            return StatusCode(500, new ParcelCreationResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// 接收DWS数据
    /// Receive DWS data for a parcel
    /// </summary>
    /// <param name="request">DWS数据请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收结果</returns>
    [HttpPost("receive-dws")]
    public async Task<ActionResult<DwsDataResponse>> ReceiveDwsData(
        [FromBody] DwsDataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "收到DWS数据 - ParcelId: {ParcelId}, Weight: {Weight}g",
                request.ParcelId, request.Weight);

            var dwsData = new DwsData
            {
                Barcode = request.Barcode ?? string.Empty,
                Weight = request.Weight,
                Length = request.Length,
                Width = request.Width,
                Height = request.Height,
                Volume = request.Volume
            };

            var success = await _orchestrationService.ReceiveDwsDataAsync(
                request.ParcelId,
                dwsData,
                cancellationToken);

            if (success)
            {
                return Ok(new DwsDataResponse
                {
                    Success = true,
                    ParcelId = request.ParcelId,
                    Message = "DWS数据已接收，开始处理"
                });
            }
            else
            {
                return NotFound(new DwsDataResponse
                {
                    Success = false,
                    ParcelId = request.ParcelId,
                    Message = "包裹不存在或已关闭"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接收DWS数据失败: {ParcelId}", request.ParcelId);
            return StatusCode(500, new DwsDataResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                Message = ex.Message
            });
        }
    }
}

/// <summary>
/// 包裹创建请求
/// Parcel creation request from sorting machine
/// </summary>
public class ParcelCreationRequest
{
    public required string ParcelId { get; set; }
    public required string CartNumber { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// 包裹创建响应
/// Parcel creation response
/// </summary>
public class ParcelCreationResponse
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}

/// <summary>
/// DWS数据请求
/// DWS data request
/// </summary>
public class DwsDataRequest
{
    public required string ParcelId { get; set; }
    public string? Barcode { get; set; }
    public decimal Weight { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Volume { get; set; }
}

/// <summary>
/// DWS数据响应
/// DWS data response
/// </summary>
public class DwsDataResponse
{
    public bool Success { get; set; }
    public required string ParcelId { get; set; }
    public required string Message { get; set; }
}
