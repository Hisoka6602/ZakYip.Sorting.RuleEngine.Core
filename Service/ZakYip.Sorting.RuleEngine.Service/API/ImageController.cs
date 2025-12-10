using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.ValueObjects;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 图片管理控制器
/// Image management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("图片管理接口，提供图片路径批量更新等功能")]
public class ImageController : ControllerBase
{
    private readonly ImagePathService _imagePathService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        ImagePathService imagePathService,
        ILogger<ImageController> logger)
    {
        _imagePathService = imagePathService;
        _logger = logger;
    }

    /// <summary>
    /// 批量更新图片路径
    /// Bulk update image paths (for disk migration scenarios)
    /// </summary>
    /// <param name="request">更新请求 / Update request</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>更新结果 / Update result</returns>
    /// <response code="200">更新成功 / Update successful</response>
    /// <response code="400">请求参数无效 / Invalid request parameters</response>
    /// <response code="500">服务器内部错误 / Internal server error</response>
    [HttpPost("bulk-update-paths")]
    [SwaggerOperation(
        Summary = "批量更新图片路径",
        Description = "用于处理磁盘迁移等场景，例如从D盘迁移到E盘。此操作使用SQL REPLACE函数高效处理，可支持数千万到数亿条记录的更新。",
        OperationId = "BulkUpdateImagePaths",
        Tags = new[] { "Image" }
    )]
    [SwaggerResponse(200, "更新成功", typeof(BulkUpdateImagePathsResponse))]
    [SwaggerResponse(400, "请求参数无效")]
    [SwaggerResponse(500, "服务器内部错误")]
    public async Task<ActionResult<BulkUpdateImagePathsResponse>> BulkUpdateImagePathsAsync(
        [FromBody, SwaggerRequestBody("包含旧路径前缀和新路径前缀的请求体", Required = true)] BulkUpdateImagePathsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OldPrefix))
            {
                return BadRequest(new { error = "OldPrefix cannot be null or empty" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPrefix))
            {
                return BadRequest(new { error = "NewPrefix cannot be null or empty" });
            }

            _logger.LogInformation("Received bulk update request: OldPrefix={OldPrefix}, NewPrefix={NewPrefix}", 
                request.OldPrefix, request.NewPrefix);

            var updatedCount = await _imagePathService.BulkUpdateImagePathsAsync(
                request.OldPrefix, 
                request.NewPrefix, 
                cancellationToken);

            return Ok(new BulkUpdateImagePathsResponse
            {
                Success = true,
                UpdatedCount = updatedCount,
                Message = $"Successfully updated {updatedCount} records"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update image paths");
            return StatusCode(500, new { error = "Failed to bulk update image paths", details = ex.Message });
        }
    }

    /// <summary>
    /// 将本地路径转换为API URL
    /// Convert local path to API URL
    /// </summary>
    /// <param name="request">转换请求 / Conversion request</param>
    /// <returns>转换后的URL / Converted URL</returns>
    /// <response code="200">转换成功 / Conversion successful</response>
    /// <response code="400">请求参数无效 / Invalid request parameters</response>
    [HttpPost("convert-path-to-url")]
    [SwaggerOperation(
        Summary = "将本地路径转换为API URL",
        Description = "将图片的本地文件路径转换为可通过API访问的HTTP URL",
        OperationId = "ConvertPathToUrl",
        Tags = new[] { "Image" }
    )]
    [SwaggerResponse(200, "转换成功", typeof(ConvertPathToUrlResponse))]
    [SwaggerResponse(400, "请求参数无效")]
    public ActionResult<ConvertPathToUrlResponse> ConvertPathToUrl(
        [FromBody, SwaggerRequestBody("包含本地路径和基础URL的请求体", Required = true)] ConvertPathToUrlRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.LocalPath))
            {
                return BadRequest(new { error = "LocalPath cannot be null or empty" });
            }

            if (string.IsNullOrWhiteSpace(request.BaseUrl))
            {
                return BadRequest(new { error = "BaseUrl cannot be null or empty" });
            }

            var url = _imagePathService.ConvertLocalPathToUrl(request.LocalPath, request.BaseUrl);

            return Ok(new ConvertPathToUrlResponse
            {
                LocalPath = request.LocalPath,
                Url = url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert path to URL");
            return BadRequest(new { error = "Failed to convert path to URL", details = ex.Message });
        }
    }
}

/// <summary>
/// 批量更新图片路径请求
/// Bulk update image paths request
/// </summary>
public class BulkUpdateImagePathsRequest
{
    /// <summary>
    /// 旧路径前缀（例如：D:\）
    /// Old path prefix (e.g., D:\)
    /// </summary>
    [SwaggerSchema("旧路径前缀，例如 D:\\images\\")]
    public string OldPrefix { get; set; } = string.Empty;

    /// <summary>
    /// 新路径前缀（例如：E:\）
    /// New path prefix (e.g., E:\)
    /// </summary>
    [SwaggerSchema("新路径前缀，例如 E:\\images\\")]
    public string NewPrefix { get; set; } = string.Empty;
}

/// <summary>
/// 批量更新图片路径响应
/// Bulk update image paths response
/// </summary>
public class BulkUpdateImagePathsResponse
{
    /// <summary>
    /// 是否成功
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 更新的记录数
    /// Number of records updated
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// 消息
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 路径转URL请求
/// Convert path to URL request
/// </summary>
public class ConvertPathToUrlRequest
{
    /// <summary>
    /// 本地路径
    /// Local path
    /// </summary>
    [SwaggerSchema("本地文件路径，例如 D:\\images\\2024\\11\\12\\image001.jpg")]
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// API基础URL
    /// Base API URL
    /// </summary>
    [SwaggerSchema("API基础URL，例如 http://api.example.com/images")]
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// 路径转URL响应
/// Convert path to URL response
/// </summary>
public class ConvertPathToUrlResponse
{
    /// <summary>
    /// 原始本地路径
    /// Original local path
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// 转换后的URL
    /// Converted URL
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
