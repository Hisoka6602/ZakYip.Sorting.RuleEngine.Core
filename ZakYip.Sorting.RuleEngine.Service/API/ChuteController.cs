using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 格口管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("格口管理接口，提供格口的增删改查功能")]
public class ChuteController : ControllerBase
{
    private readonly IChuteRepository _chuteRepository;
    private readonly ConfigurationCacheService _cacheService;
    private readonly ILogger<ChuteController> _logger;
    private readonly IPublisher _publisher;

    public ChuteController(
        IChuteRepository chuteRepository,
        ConfigurationCacheService cacheService,
        ILogger<ChuteController> logger,
        IPublisher publisher)
    {
        _chuteRepository = chuteRepository;
        _cacheService = cacheService;
        _logger = logger;
        _publisher = publisher;
    }

    /// <summary>
    /// 获取所有格口
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口列表</returns>
    /// <response code="200">成功返回格口列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取所有格口",
        Description = "获取系统中所有格口信息，包括启用和禁用的格口（从缓存读取）",
        OperationId = "GetAllChutes",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(200, "成功返回格口列表", typeof(IEnumerable<Chute>))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var chutes = await _cacheService.GetAllChutesAsync(_chuteRepository, cancellationToken);
            return Ok(chutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有格口失败");
            return StatusCode(500, new { error = "获取格口列表失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取格口
    /// </summary>
    /// <param name="id">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口详情</returns>
    /// <response code="200">成功返回格口详情</response>
    /// <response code="404">格口不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "根据ID获取格口",
        Description = "根据格口ID获取特定格口的详细信息",
        OperationId = "GetChuteById",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(200, "成功返回格口详情", typeof(Chute))]
    [SwaggerResponse(404, "格口不存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetById(
        [SwaggerParameter("格口ID", Required = true)] long id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var chute = await _chuteRepository.GetByIdAsync(id, cancellationToken);
            if (chute == null)
            {
                return NotFound(new { error = "格口不存在", chuteId = id });
            }
            return Ok(chute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取格口失败，ID: {ChuteId}", id);
            return StatusCode(500, new { error = "获取格口失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 根据编号获取格口
    /// </summary>
    /// <param name="code">格口编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口详情</returns>
    /// <response code="200">成功返回格口详情</response>
    /// <response code="404">格口不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("code/{code}")]
    [SwaggerOperation(
        Summary = "根据编号获取格口",
        Description = "根据格口编号获取特定格口的详细信息",
        OperationId = "GetChuteByCode",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(200, "成功返回格口详情", typeof(Chute))]
    [SwaggerResponse(404, "格口不存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetByCode(
        [SwaggerParameter("格口编号", Required = true)] string code, 
        CancellationToken cancellationToken)
    {
        try
        {
            var chute = await _chuteRepository.GetByCodeAsync(code, cancellationToken);
            if (chute == null)
            {
                return NotFound(new { error = "格口不存在", chuteCode = code });
            }
            return Ok(chute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取格口失败，编号: {ChuteCode}", code);
            return StatusCode(500, new { error = "获取格口失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有启用的格口
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启用的格口列表</returns>
    /// <response code="200">成功返回启用的格口列表</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet("enabled")]
    [SwaggerOperation(
        Summary = "获取所有启用的格口",
        Description = "获取系统中所有已启用的格口信息（从缓存读取）",
        OperationId = "GetEnabledChutes",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(200, "成功返回启用的格口列表", typeof(IEnumerable<Chute>))]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> GetEnabled(CancellationToken cancellationToken)
    {
        try
        {
            var chutes = await _cacheService.GetEnabledChutesAsync(_chuteRepository, cancellationToken);
            return Ok(chutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用格口失败");
            return StatusCode(500, new { error = "获取启用格口失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 创建格口
    /// </summary>
    /// <param name="chute">格口信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的格口</returns>
    /// <response code="201">格口创建成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="409">格口编号已存在</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     POST /api/chute
    ///     {
    ///        "chuteName": "深圳格口1号",
    ///        "chuteCode": "SZ001",
    ///        "description": "深圳方向专用格口",
    ///        "isEnabled": true
    ///     }
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(
        Summary = "创建格口",
        Description = "创建新的分拣格口。格口编号不能重复。",
        OperationId = "CreateChute",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(201, "格口创建成功", typeof(Chute))]
    [SwaggerResponse(400, "请求参数错误")]
    [SwaggerResponse(409, "格口编号已存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> Create(
        [FromBody, SwaggerRequestBody("格口信息", Required = true)] Chute chute, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chute.ChuteName))
            {
                return BadRequest(new { error = "格口名称不能为空" });
            }

            // 检查编号是否已存在
            if (!string.IsNullOrWhiteSpace(chute.ChuteCode))
            {
                var existing = await _chuteRepository.GetByCodeAsync(chute.ChuteCode, cancellationToken);
                if (existing != null)
                {
                    return Conflict(new { error = "格口编号已存在", chuteCode = chute.ChuteCode });
                }
            }

            var created = await _chuteRepository.AddAsync(chute, cancellationToken);
            _logger.LogInformation("创建格口成功，ID: {ChuteId}，名称: {ChuteName}", created.ChuteId, created.ChuteName);
            
            // 发布格口创建事件
            await _publisher.Publish(new ChuteCreatedEvent
            {
                ChuteId = created.ChuteId,
                ChuteName = created.ChuteName,
                ChuteCode = created.ChuteCode,
                IsEnabled = created.IsEnabled,
                CreatedAt = DateTime.Now
            }, cancellationToken);
            
            // 重新加载缓存
            await _cacheService.ReloadChuteCacheAsync(_chuteRepository, cancellationToken);
            
            // 发布缓存失效事件
            await _publisher.Publish(new ConfigurationCacheInvalidatedEvent
            {
                CacheType = "Chute",
                Reason = "新增格口",
                InvalidatedAt = DateTime.Now
            }, cancellationToken);
            
            return CreatedAtAction(nameof(GetById), new { id = created.ChuteId }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建格口失败");
            return StatusCode(500, new { error = "创建格口失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 更新格口
    /// </summary>
    /// <param name="id">格口ID</param>
    /// <param name="chute">更新的格口信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的格口</returns>
    /// <response code="200">格口更新成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="404">格口不存在</response>
    /// <response code="409">格口编号与其他格口冲突</response>
    /// <response code="500">服务器内部错误</response>
    /// <remarks>
    /// 示例请求:
    /// 
    ///     PUT /api/chute/1
    ///     {
    ///        "chuteName": "深圳格口1号(更新)",
    ///        "chuteCode": "SZ001",
    ///        "description": "深圳方向专用格口-已更新",
    ///        "isEnabled": true
    ///     }
    /// </remarks>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "更新格口",
        Description = "更新现有格口的信息。格口ID必须与路径参数一致。",
        OperationId = "UpdateChute",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(200, "格口更新成功", typeof(Chute))]
    [SwaggerResponse(400, "请求参数错误")]
    [SwaggerResponse(404, "格口不存在")]
    [SwaggerResponse(409, "格口编号与其他格口冲突")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> Update(
        [SwaggerParameter("格口ID", Required = true)] long id, 
        [FromBody, SwaggerRequestBody("更新的格口信息", Required = true)] Chute chute, 
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _chuteRepository.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { error = "格口不存在", chuteId = id });
            }

            if (string.IsNullOrWhiteSpace(chute.ChuteName))
            {
                return BadRequest(new { error = "格口名称不能为空" });
            }

            // 检查编号是否与其他格口冲突
            if (!string.IsNullOrWhiteSpace(chute.ChuteCode))
            {
                var duplicate = await _chuteRepository.GetByCodeAsync(chute.ChuteCode, cancellationToken);
                if (duplicate != null && duplicate.ChuteId != id)
                {
                    return Conflict(new { error = "格口编号已被其他格口使用", chuteCode = chute.ChuteCode });
                }
            }

            chute.ChuteId = id;
            chute.CreatedAt = existing.CreatedAt; // 保留创建时间
            await _chuteRepository.UpdateAsync(chute, cancellationToken);
            
            // 发布格口更新事件
            await _publisher.Publish(new ChuteUpdatedEvent
            {
                ChuteId = chute.ChuteId,
                ChuteName = chute.ChuteName,
                ChuteCode = chute.ChuteCode,
                IsEnabled = chute.IsEnabled,
                UpdatedAt = DateTime.Now
            }, cancellationToken);
            
            // 重新加载缓存
            await _cacheService.ReloadChuteCacheAsync(_chuteRepository, cancellationToken);
            
            // 发布缓存失效事件
            await _publisher.Publish(new ConfigurationCacheInvalidatedEvent
            {
                CacheType = "Chute",
                Reason = "更新格口",
                InvalidatedAt = DateTime.Now
            }, cancellationToken);
            
            _logger.LogInformation("更新格口成功，ID: {ChuteId}", id);
            return Ok(chute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新格口失败，ID: {ChuteId}", id);
            return StatusCode(500, new { error = "更新格口失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 删除格口
    /// </summary>
    /// <param name="id">格口ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    /// <response code="204">格口删除成功</response>
    /// <response code="404">格口不存在</response>
    /// <response code="500">服务器内部错误</response>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "删除格口",
        Description = "根据格口ID删除指定的格口",
        OperationId = "DeleteChute",
        Tags = new[] { "Chute" }
    )]
    [SwaggerResponse(204, "格口删除成功")]
    [SwaggerResponse(404, "格口不存在")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<IActionResult> Delete(
        [SwaggerParameter("格口ID", Required = true)] long id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _chuteRepository.GetByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { error = "格口不存在", chuteId = id });
            }

            await _chuteRepository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("删除格口成功，ID: {ChuteId}", id);
            
            // 发布格口删除事件
            await _publisher.Publish(new ChuteDeletedEvent
            {
                ChuteId = existing.ChuteId,
                ChuteName = existing.ChuteName,
                ChuteCode = existing.ChuteCode,
                DeletedAt = DateTime.Now
            }, cancellationToken);
            
            // 重新加载缓存
            await _cacheService.ReloadChuteCacheAsync(_chuteRepository, cancellationToken);
            
            // 发布缓存失效事件
            await _publisher.Publish(new ConfigurationCacheInvalidatedEvent
            {
                CacheType = "Chute",
                Reason = "删除格口",
                InvalidatedAt = DateTime.Now
            }, cancellationToken);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除格口失败，ID: {ChuteId}", id);
            return StatusCode(500, new { error = "删除格口失败", message = ex.Message });
        }
    }
}
