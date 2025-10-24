using Microsoft.AspNetCore.Mvc;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 格口管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChuteController : ControllerBase
{
    private readonly IChuteRepository _chuteRepository;
    private readonly ILogger<ChuteController> _logger;

    public ChuteController(
        IChuteRepository chuteRepository,
        ILogger<ChuteController> logger)
    {
        _chuteRepository = chuteRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有格口
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var chutes = await _chuteRepository.GetAllAsync(cancellationToken);
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
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
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
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
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
    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled(CancellationToken cancellationToken)
    {
        try
        {
            var chutes = await _chuteRepository.GetEnabledChutesAsync(cancellationToken);
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
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Chute chute, CancellationToken cancellationToken)
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
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Chute chute, CancellationToken cancellationToken)
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
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
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除格口失败，ID: {ChuteId}", id);
            return StatusCode(500, new { error = "删除格口失败", message = ex.Message });
        }
    }
}
