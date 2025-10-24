using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Application.DTOs;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 包裹处理API控制器
/// Parcel processing API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    /// Process a single parcel
    /// </summary>
    /// <param name="request">包裹处理请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    [HttpPost("process")]
    public async Task<ActionResult<ParcelProcessResponse>> ProcessParcel(
        [FromBody] ParcelProcessRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到包裹处理请求: {ParcelId}", request.ParcelId);

            var response = await _parcelProcessingService.ProcessParcelAsync(request, cancellationToken);

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
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// 批量处理包裹
    /// Process multiple parcels in batch
    /// </summary>
    /// <param name="requests">包裹处理请求列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果列表</returns>
    [HttpPost("process/batch")]
    public async Task<ActionResult<IEnumerable<ParcelProcessResponse>>> ProcessParcels(
        [FromBody] IEnumerable<ParcelProcessRequest> requests,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到批量包裹处理请求，数量: {Count}", requests.Count());

            var responses = await _parcelProcessingService.ProcessParcelsAsync(requests, cancellationToken);

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处理包裹异常");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
