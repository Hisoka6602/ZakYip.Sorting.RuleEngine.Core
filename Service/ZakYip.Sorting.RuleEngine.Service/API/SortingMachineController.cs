using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 分拣机信号接收API控制器
/// 注意：此HTTP API仅用于测试和调试，生产环境中分拣程序和DWS应使用TCP或SignalR通信
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("分拣机管理 / Sorting Management")]
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
    /// 注意：仅用于测试，生产环境请使用SignalR Hub (/hubs/sorting) 或 TCP适配器
    /// </summary>
    /// <param name="request">包裹创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建结果</returns>
    /// <response code="200">包裹处理空间创建成功</response>
    /// <response code="400">包裹ID已存在或创建失败</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/sortingmachine/create-parcel
    ///     {
    ///        "parcelId": "PKG20231101001",
    ///        "cartNumber": "CART001",
    ///        "barcode": "1234567890123"
    ///     }
    /// </remarks>
    [HttpPost("create-parcel")]
    [SwaggerOperation(
        Summary = "接收分拣程序信号，创建包裹处理空间",
        Description = "接收分拣机信号，创建包裹处理空间等待DWS数据。仅用于测试，生产环境请使用SignalR Hub。",
        OperationId = "CreateParcel",
        Tags = new[] { "SortingMachine" }
    )]
    [SwaggerResponse(200, "包裹处理空间创建成功", typeof(ParcelCreationResponse))]
    [SwaggerResponse(400, "包裹ID已存在或创建失败", typeof(ParcelCreationResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ParcelCreationResponse))]
    public async Task<ActionResult<ParcelCreationResponse>> CreateParcel(
        [FromBody, SwaggerRequestBody("包裹创建请求", Required = true)] ParcelCreationRequest request,
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
                cancellationToken).ConfigureAwait(false);

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
    /// 注意：仅用于测试，生产环境请使用SignalR Hub (/hubs/dws) 或 TCP适配器
    /// </summary>
    /// <param name="request">DWS数据请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收结果</returns>
    /// <response code="200">DWS数据接收成功</response>
    /// <response code="404">包裹不存在或已关闭</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/sortingmachine/receive-dws
    ///     {
    ///        "parcelId": "PKG20231101001",
    ///        "barcode": "1234567890123",
    ///        "weight": 2500.5,
    ///        "length": 300,
    ///        "width": 200,
    ///        "height": 150,
    ///        "volume": 9000
    ///     }
    /// </remarks>
    [HttpPost("receive-dws")]
    [SwaggerOperation(
        Summary = "接收DWS数据",
        Description = "接收DWS扫描的包裹数据，触发分拣处理。仅用于测试，生产环境请使用SignalR Hub。",
        OperationId = "ReceiveDwsData",
        Tags = new[] { "SortingMachine" }
    )]
    [SwaggerResponse(200, "DWS数据接收成功", typeof(DwsDataResponse))]
    [SwaggerResponse(404, "包裹不存在或已关闭", typeof(DwsDataResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(DwsDataResponse))]
    public async Task<ActionResult<DwsDataResponse>> ReceiveDwsData(
        [FromBody, SwaggerRequestBody("DWS数据请求", Required = true)] DwsDataRequest request,
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
                cancellationToken).ConfigureAwait(false);

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
/// </summary>
[SwaggerSchema(Description = "包裹创建请求，用于创建包裹处理空间")]
public class ParcelCreationRequest
{
    /// <summary>
    /// 包裹ID
    /// </summary>
    public required string ParcelId { get; set; }
    
    /// <summary>
    /// 小车号
    /// </summary>
    public required string CartNumber { get; set; }
    
    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; set; }
}

/// <summary>
/// 通用操作响应基类 / Base class for operation responses
/// 消除影分身：ParcelCreationResponse 和 DwsDataResponse 结构完全相同
/// Shadow clone elimination: ParcelCreationResponse and DwsDataResponse have identical structure
/// </summary>
public abstract class OperationResponseBase
{
    /// <summary>
    /// 是否成功 / Success flag
    /// 示例: true
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// 包裹ID / Parcel ID
    /// 示例: PKG20231101001
    /// </summary>
    public required string ParcelId { get; init; }
    
    /// <summary>
    /// 消息 / Message
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// 包裹创建响应 / Parcel creation response
/// </summary>
[SwaggerSchema(Description = "包裹创建响应")]
public sealed class ParcelCreationResponse : OperationResponseBase
{
}

/// <summary>
/// DWS数据请求 / DWS data request
/// </summary>
[SwaggerSchema(Description = "DWS数据请求")]
public class DwsDataRequest
{
    /// <summary>
    /// 包裹ID / Parcel ID
    /// 示例: PKG20231101001
    /// </summary>
    public required string ParcelId { get; set; }
    
    /// <summary>
    /// 条码 / Barcode
    /// 示例: 1234567890123
    /// </summary>
    public string? Barcode { get; set; }
    
    /// <summary>
    /// 重量（克） / Weight (grams)
    /// 示例: 2500.5
    /// </summary>
    public decimal Weight { get; set; }
    
    /// <summary>
    /// 长度（毫米） / Length (mm)
    /// 示例: 300
    /// </summary>
    public decimal Length { get; set; }
    
    /// <summary>
    /// 宽度（毫米） / Width (mm)
    /// 示例: 200
    /// </summary>
    public decimal Width { get; set; }
    
    /// <summary>
    /// 高度（毫米） / Height (mm)
    /// 示例: 150
    /// </summary>
    public decimal Height { get; set; }
    
    /// <summary>
    /// 体积（立方厘米） / Volume (cubic cm)
    /// 示例: 9000
    /// </summary>
    public decimal Volume { get; set; }
}

/// <summary>
/// DWS数据响应 / DWS data response
/// </summary>
[SwaggerSchema(Description = "DWS数据响应")]
public sealed class DwsDataResponse : OperationResponseBase
{
}
