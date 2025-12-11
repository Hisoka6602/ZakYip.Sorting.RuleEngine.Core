using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 包裹处理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("包裹处理接口，提供包裹的单个和批量处理功能")]
public class ParcelController : ControllerBase
{
    private readonly IParcelProcessingService _parcelProcessingService;
    private readonly ILogger<ParcelController> _logger;

    public ParcelController(
        IParcelProcessingService parcelProcessingService,
        ILogger<ParcelController> logger)
    {
        _parcelProcessingService = parcelProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// 处理单个包裹
    /// </summary>
    /// <param name="request">包裹处理请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    /// <response code="200">包裹处理成功</response>
    /// <response code="400">包裹处理失败</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/parcel/process
    ///     {
    ///        "parcelId": "PKG20231101001",
    ///        "cartNumber": "CART001",
    ///        "barcode": "1234567890123",
    ///        "weight": 2500.5,
    ///        "length": 300,
    ///        "width": 200,
    ///        "height": 150,
    ///        "volume": 9000
    ///     }
    /// </remarks>
    [HttpPost("process")]
    [SwaggerOperation(
        Summary = "处理单个包裹",
        Description = "根据包裹信息和分拣规则，为包裹分配目标格口",
        OperationId = "ProcessParcel",
        Tags = new[] { "Parcel" }
    )]
    [SwaggerResponse(200, "包裹处理成功", typeof(ParcelProcessResponse))]
    [SwaggerResponse(400, "包裹处理失败", typeof(ParcelProcessResponse))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ParcelProcessResponse))]
    public async Task<ActionResult<ParcelProcessResponse>> ProcessParcel(
        [FromBody, SwaggerRequestBody("包裹处理请求", Required = true)] ParcelProcessRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到包裹处理请求: {ParcelId}", request.ParcelId);

            var response = await _parcelProcessingService.ProcessParcelAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理包裹异常: {ParcelId}", request.ParcelId);
            return StatusCode(500, new ParcelProcessResponse
            {
                Success = false,
                ParcelId = request.ParcelId,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = 0
            });
        }
    }

    /// <summary>
    /// 批量处理包裹
    /// </summary>
    /// <param name="requests">包裹处理请求列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果列表</returns>
    /// <response code="200">批量处理完成（包含每个包裹的处理结果）</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/parcel/process/batch
    ///     [
    ///       {
    ///         "parcelId": "PKG20231101001",
    ///         "cartNumber": "CART001",
    ///         "barcode": "1234567890123",
    ///         "weight": 2500.5,
    ///         "length": 300,
    ///         "width": 200,
    ///         "height": 150,
    ///         "volume": 9000
    ///       },
    ///       {
    ///         "parcelId": "PKG20231101002",
    ///         "cartNumber": "CART002",
    ///         "barcode": "1234567890124",
    ///         "weight": 1800.0,
    ///         "length": 250,
    ///         "width": 180,
    ///         "height": 120,
    ///         "volume": 5400
    ///       }
    ///     ]
    /// </remarks>
    [HttpPost("process/batch")]
    [SwaggerOperation(
        Summary = "批量处理包裹",
        Description = "批量处理多个包裹，为每个包裹分配目标格口。返回每个包裹的处理结果。",
        OperationId = "ProcessParcels",
        Tags = new[] { "Parcel" }
    )]
    [SwaggerResponse(200, "批量处理完成", typeof(IEnumerable<ParcelProcessResponse>))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<IEnumerable<ParcelProcessResponse>>> ProcessParcels(
        [FromBody, SwaggerRequestBody("包裹处理请求列表", Required = true)] IEnumerable<ParcelProcessRequest> requests,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到批量包裹处理请求，数量: {Count}", requests.Count());

            var responses = await _parcelProcessingService.ProcessParcelsAsync(requests, cancellationToken).ConfigureAwait(false);

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处理包裹异常");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
