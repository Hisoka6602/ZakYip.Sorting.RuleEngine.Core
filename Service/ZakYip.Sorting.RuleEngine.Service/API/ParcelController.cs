using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 包裹信息API控制器
/// Parcel information API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("包裹管理接口，提供包裹查询、搜索和生命周期管理功能")]
public class ParcelController : ControllerBase
{
    private readonly IParcelInfoRepository _parcelRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ILogger<ParcelController> _logger;

    public ParcelController(
        IParcelInfoRepository parcelRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ILogger<ParcelController> logger)
    {
        _parcelRepository = parcelRepository;
        _lifecycleRepository = lifecycleRepository;
        _logger = logger;
    }

    /// <summary>
    /// 根据包裹ID获取包裹详情
    /// Get parcel details by parcel ID
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹详细信息 / Parcel details</returns>
    /// <response code="200">成功返回包裹信息 / Successfully returns parcel information</response>
    /// <response code="404">包裹不存在 / Parcel not found</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpGet("{parcelId}")]
    [SwaggerOperation(
        Summary = "获取包裹详情",
        Description = "根据包裹ID获取包裹的详细信息，包括DWS数据、分拣信息、状态等",
        OperationId = "GetParcel",
        Tags = new[] { "Parcel" }
    )]
    [ProducesResponseType(typeof(ParcelInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ParcelInfo>> GetParcel(
        [FromRoute] string parcelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取包裹详情: ParcelId={ParcelId}", parcelId);
            
            var parcel = await _parcelRepository.GetByIdAsync(parcelId, cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogWarning("包裹不存在: ParcelId={ParcelId}", parcelId);
                return NotFound(new { error = "包裹不存在", parcelId });
            }

            return Ok(parcel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包裹详情时发生错误: ParcelId={ParcelId}", parcelId);
            return StatusCode(500, new { error = "获取包裹详情时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 搜索包裹（支持分页和过滤）
    /// Search parcels (supports pagination and filtering)
    /// </summary>
    /// <param name="status">状态过滤 / Status filter</param>
    /// <param name="lifecycleStage">生命周期阶段过滤 / Lifecycle stage filter</param>
    /// <param name="bagId">袋ID过滤 / Bag ID filter</param>
    /// <param name="page">页码（从1开始）/ Page number (1-based)</param>
    /// <param name="pageSize">每页大小 / Page size</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹列表和总数 / Parcel list and total count</returns>
    /// <response code="200">成功返回包裹列表 / Successfully returns parcel list</response>
    /// <response code="400">请求参数无效 / Invalid request parameters</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpGet("search")]
    [SwaggerOperation(
        Summary = "搜索包裹",
        Description = "搜索包裹，支持按状态、生命周期阶段、袋ID等条件过滤，支持分页",
        OperationId = "SearchParcels",
        Tags = new[] { "Parcel" }
    )]
    [ProducesResponseType(typeof(ParcelSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ParcelSearchResponse>> SearchParcels(
        [FromQuery] ParcelStatus? status = null,
        [FromQuery] ParcelLifecycleStage? lifecycleStage = null,
        [FromQuery] string? bagId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest(new { error = "页码必须大于0" });
            }

            if (pageSize < 1 || pageSize > 1000)
            {
                return BadRequest(new { error = "每页大小必须在1-1000之间" });
            }

            _logger.LogDebug("搜索包裹: Status={Status}, LifecycleStage={LifecycleStage}, BagId={BagId}, Page={Page}, PageSize={PageSize}",
                status, lifecycleStage, bagId, page, pageSize);

            var (items, totalCount) = await _parcelRepository.SearchAsync(
                status, lifecycleStage, bagId, null, null, page, pageSize, cancellationToken).ConfigureAwait(false);

            return Ok(new ParcelSearchResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索包裹时发生错误");
            return StatusCode(500, new { error = "搜索包裹时发生内部错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取包裹生命周期节点列表
    /// Get parcel lifecycle nodes list
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>生命周期节点列表 / Lifecycle nodes list</returns>
    /// <response code="200">成功返回生命周期节点列表 / Successfully returns lifecycle nodes list</response>
    /// <response code="404">包裹不存在 / Parcel not found</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpGet("{parcelId}/lifecycle")]
    [SwaggerOperation(
        Summary = "获取包裹生命周期",
        Description = "获取包裹的完整生命周期节点列表，按时间倒序排列",
        OperationId = "GetParcelLifecycle",
        Tags = new[] { "Parcel" }
    )]
    [ProducesResponseType(typeof(IReadOnlyList<ParcelLifecycleNodeEntity>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ParcelLifecycleNodeEntity>>> GetParcelLifecycle(
        [FromRoute] string parcelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("获取包裹生命周期: ParcelId={ParcelId}", parcelId);

            // 先检查包裹是否存在
            var parcel = await _parcelRepository.GetByIdAsync(parcelId, cancellationToken).ConfigureAwait(false);
            if (parcel == null)
            {
                _logger.LogWarning("包裹不存在: ParcelId={ParcelId}", parcelId);
                return NotFound(new { error = "包裹不存在", parcelId });
            }

            var nodes = await _lifecycleRepository.GetByParcelIdAsync(parcelId, cancellationToken).ConfigureAwait(false);

            return Ok(nodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取包裹生命周期时发生错误: ParcelId={ParcelId}", parcelId);
            return StatusCode(500, new { error = "获取生命周期时发生内部错误", message = ex.Message });
        }
    }
}

/// <summary>
/// 包裹搜索响应
/// Parcel search response
/// </summary>
public record class ParcelSearchResponse
{
    /// <summary>
    /// 包裹列表 / Parcel list
    /// </summary>
    public required IReadOnlyList<ParcelInfo> Items { get; init; }
    
    /// <summary>
    /// 总数 / Total count
    /// </summary>
    public required int TotalCount { get; init; }
    
    /// <summary>
    /// 当前页码 / Current page
    /// </summary>
    public required int Page { get; init; }
    
    /// <summary>
    /// 每页大小 / Page size
    /// </summary>
    public required int PageSize { get; init; }
    
    /// <summary>
    /// 总页数 / Total pages
    /// </summary>
    public required int TotalPages { get; init; }
}
