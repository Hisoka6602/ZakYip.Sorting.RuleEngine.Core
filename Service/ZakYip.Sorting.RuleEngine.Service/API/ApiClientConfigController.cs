using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostProcessingCenter;
using ZakYip.Sorting.RuleEngine.Service.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// ApiClient配置管理控制器
/// ApiClient Configuration Management Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("ApiClient配置管理接口")]
public class ApiClientConfigController : ControllerBase
{
    private readonly ILogger<ApiClientConfigController> _logger;
    private readonly JushuitanErpApiClient? _jushuitanErpApiClient;
    private readonly WdtWmsApiClient? _wdtWmsApiClient;
    private readonly WdtErpFlagshipApiClient? _wdtErpFlagshipApiClient;
    private readonly PostCollectionApiClient? _postCollectionApiClient;
    private readonly PostProcessingCenterApiClient? _postProcessingCenterApiClient;
    private readonly AppSettings _appSettings;
    private readonly ISystemClock _clock;
    private readonly IPostCollectionConfigRepository _postCollectionConfigRepository;
    private readonly IPostProcessingCenterConfigRepository _postProcessingCenterConfigRepository;
    private readonly IJushuitanErpConfigRepository _jushuitanErpConfigRepository;
    private readonly IWdtWmsConfigRepository _wdtWmsConfigRepository;
    private readonly IWdtErpFlagshipConfigRepository _wdtErpFlagshipConfigRepository;

    public ApiClientConfigController(
        ILogger<ApiClientConfigController> logger,
        IServiceProvider serviceProvider,
        IOptions<AppSettings> appSettings,
        ISystemClock clock,
        IPostCollectionConfigRepository postCollectionConfigRepository,
        IPostProcessingCenterConfigRepository postProcessingCenterConfigRepository,
        IJushuitanErpConfigRepository jushuitanErpConfigRepository,
        IWdtWmsConfigRepository wdtWmsConfigRepository,
        IWdtErpFlagshipConfigRepository wdtErpFlagshipConfigRepository)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _clock = clock;
        _postCollectionConfigRepository = postCollectionConfigRepository;
        _postProcessingCenterConfigRepository = postProcessingCenterConfigRepository;
        _jushuitanErpConfigRepository = jushuitanErpConfigRepository;
        _wdtWmsConfigRepository = wdtWmsConfigRepository;
        _wdtErpFlagshipConfigRepository = wdtErpFlagshipConfigRepository;
        
        // Try to get clients from DI, they may not be registered
        _jushuitanErpApiClient = serviceProvider.GetService<JushuitanErpApiClient>();
        _wdtWmsApiClient = serviceProvider.GetService<WdtWmsApiClient>();
        _wdtErpFlagshipApiClient = serviceProvider.GetService<WdtErpFlagshipApiClient>();
        _postCollectionApiClient = serviceProvider.GetService<PostCollectionApiClient>();
        _postProcessingCenterApiClient = serviceProvider.GetService<PostProcessingCenterApiClient>();
    }

    /// <summary>
    /// 获取聚水潭ERP API配置
    /// Get Jushuituan ERP API configuration
    /// </summary>
    /// <returns>当前配置</returns>
    [HttpGet("jushuitanerp")]
    [SwaggerOperation(
        Summary = "获取聚水潭ERP API配置",
        Description = "获取当前聚水潭ERP API客户端的配置参数（从LiteDB读取）",
        OperationId = "GetJushuitanErpConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<JushuitanErpConfigRequest>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<JushuitanErpConfigRequest>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<JushuitanErpConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<JushuitanErpConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<JushuitanErpConfigRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<JushuitanErpConfigRequest>), 500)]
    public async Task<ActionResult<ApiResponse<JushuitanErpConfigRequest>>> GetJushuitanErpConfig()
    {
        try
        {
            var config = await _jushuitanErpConfigRepository.GetByIdAsync(JushuitanErpConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<JushuitanErpConfigRequest>.FailureResult(
                    "聚水潭ERP配置不存在", "CONFIG_NOT_FOUND"));
            }

            var dto = new JushuitanErpConfigRequest
            {
                Name = config.Name,
                Url = config.Url,
                TimeoutMs = config.TimeoutMs,
                AppKey = MaskSecret(config.AppKey),
                AppSecret = MaskSecret(config.AppSecret),
                AccessToken = MaskSecret(config.AccessToken),
                Version = config.Version,
                IsUploadWeight = config.IsUploadWeight,
                Type = config.Type,
                IsUnLid = config.IsUnLid,
                Channel = config.Channel,
                DefaultWeight = config.DefaultWeight,
                IsEnabled = config.IsEnabled,
                Description = config.Description
            };

            return Ok(ApiResponse<JushuitanErpConfigRequest>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聚水潭ERP配置时发生错误");
            return StatusCode(500, ApiResponse<JushuitanErpConfigRequest>.FailureResult(
                "获取配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新聚水潭ERP API配置
    /// Update Jushuituan ERP API configuration
    /// </summary>
    /// <param name="request">配置请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("jushuitanerp")]
    [SwaggerOperation(
        Summary = "更新聚水潭ERP API配置",
        Description = "更新聚水潭ERP API客户端的配置参数（保存到LiteDB，支持热更新）",
        OperationId = "UpdateJushuitanErpConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<string>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> UpdateJushuitanErpConfig([FromBody] JushuitanErpConfigRequest request)
    {
        try
        {
            var existingConfig = await _jushuitanErpConfigRepository.GetByIdAsync(JushuitanErpConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new JushuitanErpConfig
            {
                ConfigId = JushuitanErpConfig.SingletonId,
                Name = request.Name,
                Url = request.Url,
                TimeoutMs = request.TimeoutMs,
                AppKey = request.AppKey,
                AppSecret = request.AppSecret,
                AccessToken = request.AccessToken,
                Version = request.Version,
                IsUploadWeight = request.IsUploadWeight,
                Type = request.Type,
                IsUnLid = request.IsUnLid,
                Channel = request.Channel,
                DefaultWeight = request.DefaultWeight,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _jushuitanErpConfigRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建聚水潭ERP API配置成功");
            }
            else
            {
                success = await _jushuitanErpConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新聚水潭ERP API配置成功");
            }
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    existingConfig == null ? "创建配置失败" : "更新配置失败", 
                    existingConfig == null ? "CREATE_FAILED" : "UPDATE_FAILED"));
            }

            return Ok(ApiResponse<string>.SuccessResult(
                existingConfig == null ? "配置创建成功" : "配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新聚水潭ERP配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 获取旺店通WMS API配置
    /// Get WDT WMS API configuration
    /// </summary>
    /// <returns>当前配置</returns>
    [HttpGet("wdtwms")]
    [SwaggerOperation(
        Summary = "获取旺店通WMS API配置",
        Description = "获取当前旺店通WMS API客户端的配置参数（从LiteDB读取）",
        OperationId = "GetWdtWmsConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WdtWmsConfigRequest>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<WdtWmsConfigRequest>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WdtWmsConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<WdtWmsConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WdtWmsConfigRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<WdtWmsConfigRequest>), 500)]
    public async Task<ActionResult<ApiResponse<WdtWmsConfigRequest>>> GetWdtWmsConfig()
    {
        try
        {
            var config = await _wdtWmsConfigRepository.GetByIdAsync(WdtWmsConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<WdtWmsConfigRequest>.FailureResult(
                    "旺店通WMS配置不存在", "CONFIG_NOT_FOUND"));
            }

            var dto = new WdtWmsConfigRequest
            {
                Name = config.Name,
                Url = config.Url,
                Sid = MaskSecret(config.Sid),
                AppKey = MaskSecret(config.AppKey),
                AppSecret = MaskSecret(config.AppSecret),
                Method = config.Method,
                TimeoutMs = config.TimeoutMs,
                MustIncludeBoxBarcode = config.MustIncludeBoxBarcode,
                DefaultWeight = config.DefaultWeight,
                IsEnabled = config.IsEnabled,
                Description = config.Description
            };

            return Ok(ApiResponse<WdtWmsConfigRequest>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取旺店通WMS配置时发生错误");
            return StatusCode(500, ApiResponse<WdtWmsConfigRequest>.FailureResult(
                "获取配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新旺店通WMS API配置
    /// Update WDT WMS API configuration
    /// </summary>
    /// <param name="request">配置请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("wdtwms")]
    [SwaggerOperation(
        Summary = "更新旺店通WMS API配置",
        Description = "更新旺店通WMS API客户端的配置参数（保存到LiteDB，支持热更新）",
        OperationId = "UpdateWdtWmsConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<string>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> UpdateWdtWmsConfig([FromBody] WdtWmsConfigRequest request)
    {
        try
        {
            var existingConfig = await _wdtWmsConfigRepository.GetByIdAsync(WdtWmsConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new WdtWmsConfig
            {
                ConfigId = WdtWmsConfig.SingletonId,
                Name = request.Name,
                Url = request.Url,
                Sid = request.Sid,
                AppKey = request.AppKey,
                AppSecret = request.AppSecret,
                Method = request.Method,
                TimeoutMs = request.TimeoutMs,
                MustIncludeBoxBarcode = request.MustIncludeBoxBarcode,
                DefaultWeight = request.DefaultWeight,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _wdtWmsConfigRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建旺店通WMS API配置成功");
            }
            else
            {
                success = await _wdtWmsConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新旺店通WMS API配置成功");
            }
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    existingConfig == null ? "创建配置失败" : "更新配置失败",
                    existingConfig == null ? "CREATE_FAILED" : "UPDATE_FAILED"));
            }

            return Ok(ApiResponse<string>.SuccessResult(
                existingConfig == null ? "配置创建成功" : "配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新旺店通WMS配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 获取旺店通ERP旗舰版 API配置
    /// Get WDT ERP Flagship API configuration
    /// </summary>
    /// <returns>当前配置</returns>
    [HttpGet("wdterpflagship")]
    [SwaggerOperation(
        Summary = "获取旺店通ERP旗舰版 API配置",
        Description = "获取当前旺店通ERP旗舰版 API客户端的配置参数（从LiteDB读取）",
        OperationId = "GetWdtErpFlagshipConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WdtErpFlagshipConfigRequest>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<WdtErpFlagshipConfigRequest>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<WdtErpFlagshipConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<WdtErpFlagshipConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WdtErpFlagshipConfigRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<WdtErpFlagshipConfigRequest>), 500)]
    public async Task<ActionResult<ApiResponse<WdtErpFlagshipConfigRequest>>> GetWdtErpFlagshipConfig()
    {
        try
        {
            var config = await _wdtErpFlagshipConfigRepository.GetByIdAsync(WdtErpFlagshipConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<WdtErpFlagshipConfigRequest>.FailureResult(
                    "旺店通ERP旗舰版配置不存在", "CONFIG_NOT_FOUND"));
            }

            var dto = new WdtErpFlagshipConfigRequest
            {
                Name = config.Name,
                Url = config.Url,
                Key = MaskSecret(config.Key),
                Appsecret = MaskSecret(config.Appsecret),
                Sid = MaskSecret(config.Sid),
                Method = config.Method,
                V = config.V,
                Salt = MaskSecret(config.Salt),
                PackagerId = config.PackagerId,
                PackagerNo = config.PackagerNo,
                OperateTableName = config.OperateTableName,
                Force = config.Force,
                TimeoutMs = config.TimeoutMs,
                IsEnabled = config.IsEnabled,
                Description = config.Description
            };

            return Ok(ApiResponse<WdtErpFlagshipConfigRequest>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取旺店通ERP旗舰版配置时发生错误");
            return StatusCode(500, ApiResponse<WdtErpFlagshipConfigRequest>.FailureResult(
                "获取配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新旺店通ERP旗舰版 API配置
    /// Update WDT ERP Flagship API configuration
    /// </summary>
    /// <param name="request">配置请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("wdterpflagship")]
    [SwaggerOperation(
        Summary = "更新旺店通ERP旗舰版 API配置",
        Description = "更新旺店通ERP旗舰版 API客户端的配置参数（保存到LiteDB，支持热更新）",
        OperationId = "UpdateWdtErpFlagshipConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<string>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> UpdateWdtErpFlagshipConfig([FromBody] WdtErpFlagshipConfigRequest request)
    {
        try
        {
            var existingConfig = await _wdtErpFlagshipConfigRepository.GetByIdAsync(WdtErpFlagshipConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new WdtErpFlagshipConfig
            {
                ConfigId = WdtErpFlagshipConfig.SingletonId,
                Name = request.Name,
                Url = request.Url,
                Key = request.Key,
                Appsecret = request.Appsecret,
                Sid = request.Sid,
                Method = request.Method,
                V = request.V,
                Salt = request.Salt,
                PackagerId = request.PackagerId,
                PackagerNo = request.PackagerNo,
                OperateTableName = request.OperateTableName,
                Force = request.Force,
                TimeoutMs = request.TimeoutMs,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _wdtErpFlagshipConfigRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建旺店通ERP旗舰版 API配置成功");
            }
            else
            {
                success = await _wdtErpFlagshipConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新旺店通ERP旗舰版 API配置成功");
            }
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    existingConfig == null ? "创建配置失败" : "更新配置失败",
                    existingConfig == null ? "CREATE_FAILED" : "UPDATE_FAILED"));
            }

            return Ok(ApiResponse<string>.SuccessResult(
                existingConfig == null ? "配置创建成功" : "配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新旺店通ERP旗舰版配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 屏蔽敏感信息（只显示前3位和后3位）
    /// Mask sensitive information (show only first 3 and last 3 characters)
    /// </summary>
    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length <= 6)
        {
            return "***";
        }

        var firstPart = secret.Substring(0, 3);
        var lastPart = secret.Substring(secret.Length - 3);
        return $"{firstPart}***{lastPart}";
    }

    /// <summary>
    /// 获取邮政分揽投机构 API配置
    /// Get Postal Collection API configuration
    /// </summary>
    /// <returns>当前配置</returns>
    [HttpGet("postcollection")]
    [SwaggerOperation(
        Summary = "获取邮政分揽投机构 API配置",
        Description = "获取当前邮政分揽投机构 API客户端的完整配置参数（从LiteDB读取）",
        OperationId = "GetPostCollectionConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<PostCollectionConfigRequest>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<PostCollectionConfigRequest>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<PostCollectionConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<PostCollectionConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PostCollectionConfigRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PostCollectionConfigRequest>), 500)]
    public async Task<ActionResult<ApiResponse<PostCollectionConfigRequest>>> GetPostCollectionConfig()
    {
        try
        {
            var config = await _postCollectionConfigRepository.GetByIdAsync(PostCollectionConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<PostCollectionConfigRequest>.FailureResult(
                    "邮政分揽投机构配置不存在", "CONFIG_NOT_FOUND"));
            }

            var dto = new PostCollectionConfigRequest
            {
                Name = config.Name,
                Url = config.Url,
                WorkshopCode = config.WorkshopCode,
                DeviceId = config.DeviceId,
                CompanyName = config.CompanyName,
                DeviceBarcode = config.DeviceBarcode,
                OrganizationNumber = config.OrganizationNumber,
                EmployeeNumber = config.EmployeeNumber,
                TimeoutMs = config.TimeoutMs,
                IsEnabled = config.IsEnabled,
                Description = config.Description
            };

            return Ok(ApiResponse<PostCollectionConfigRequest>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取邮政分揽投机构配置时发生错误");
            return StatusCode(500, ApiResponse<PostCollectionConfigRequest>.FailureResult(
                "获取配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新邮政分揽投机构 API配置
    /// Update Postal Collection API configuration
    /// </summary>
    /// <param name="request">配置请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("postcollection")]
    [SwaggerOperation(
        Summary = "更新邮政分揽投机构 API配置",
        Description = "更新邮政分揽投机构 API客户端的配置参数（保存到LiteDB，支持热更新）",
        OperationId = "UpdatePostCollectionConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<string>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> UpdatePostCollectionConfig([FromBody] PostCollectionConfigRequest request)
    {
        try
        {
            var existingConfig = await _postCollectionConfigRepository.GetByIdAsync(PostCollectionConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new PostCollectionConfig
            {
                ConfigId = PostCollectionConfig.SingletonId,
                Name = request.Name,
                Url = request.Url,
                WorkshopCode = request.WorkshopCode,
                DeviceId = request.DeviceId,
                CompanyName = request.CompanyName,
                DeviceBarcode = request.DeviceBarcode,
                OrganizationNumber = request.OrganizationNumber,
                EmployeeNumber = request.EmployeeNumber,
                TimeoutMs = request.TimeoutMs,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _postCollectionConfigRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建邮政分揽投机构 API配置成功");
            }
            else
            {
                success = await _postCollectionConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新邮政分揽投机构 API配置成功");
            }
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    existingConfig == null ? "创建配置失败" : "更新配置失败",
                    existingConfig == null ? "CREATE_FAILED" : "UPDATE_FAILED"));
            }

            return Ok(ApiResponse<string>.SuccessResult(
                existingConfig == null ? "配置创建成功" : "配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新邮政分揽投机构配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 获取邮政处理中心 API配置
    /// Get Postal Processing Center API configuration
    /// </summary>
    /// <returns>当前配置</returns>
    [HttpGet("postprocessingcenter")]
    [SwaggerOperation(
        Summary = "获取邮政处理中心 API配置",
        Description = "获取当前邮政处理中心 API客户端的配置参数（从LiteDB读取）",
        OperationId = "GetPostProcessingCenterConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<PostProcessingCenterConfigRequest>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<PostProcessingCenterConfigRequest>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<PostProcessingCenterConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<PostProcessingCenterConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PostProcessingCenterConfigRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<PostProcessingCenterConfigRequest>), 500)]
    public async Task<ActionResult<ApiResponse<PostProcessingCenterConfigRequest>>> GetPostProcessingCenterConfig()
    {
        try
        {
            var config = await _postProcessingCenterConfigRepository.GetByIdAsync(PostProcessingCenterConfig.SingletonId).ConfigureAwait(false);
            
            if (config == null)
            {
                return NotFound(ApiResponse<PostProcessingCenterConfigRequest>.FailureResult(
                    "邮政处理中心配置不存在", "CONFIG_NOT_FOUND"));
            }

            var dto = new PostProcessingCenterConfigRequest
            {
                Name = config.Name,
                Url = config.Url,
                WorkshopCode = config.WorkshopCode,
                DeviceId = config.DeviceId,
                CompanyName = config.CompanyName,
                DeviceBarcode = config.DeviceBarcode,
                OrganizationNumber = config.OrganizationNumber,
                EmployeeNumber = config.EmployeeNumber,
                TimeoutMs = config.TimeoutMs,
                IsEnabled = config.IsEnabled,
                Description = config.Description
            };

            return Ok(ApiResponse<PostProcessingCenterConfigRequest>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取邮政处理中心配置时发生错误");
            return StatusCode(500, ApiResponse<PostProcessingCenterConfigRequest>.FailureResult(
                "获取配置失败", "GET_CONFIG_FAILED"));
        }
    }

    /// <summary>
    /// 更新邮政处理中心 API配置
    /// Update Postal Processing Center API configuration
    /// </summary>
    /// <param name="request">配置请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("postprocessingcenter")]
    [SwaggerOperation(
        Summary = "更新邮政处理中心 API配置",
        Description = "更新邮政处理中心 API客户端的配置参数（保存到LiteDB，支持热更新）",
        OperationId = "UpdatePostProcessingCenterConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "配置不存在", typeof(ApiResponse<string>))]
    [SwaggerResponse(500, "服务器内部错误", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<ActionResult<ApiResponse<string>>> UpdatePostProcessingCenterConfig([FromBody] PostProcessingCenterConfigRequest request)
    {
        try
        {
            var existingConfig = await _postProcessingCenterConfigRepository.GetByIdAsync(PostProcessingCenterConfig.SingletonId).ConfigureAwait(false);
            
            var now = _clock.LocalNow;
            var updatedConfig = new PostProcessingCenterConfig
            {
                ConfigId = PostProcessingCenterConfig.SingletonId,
                Name = request.Name,
                Url = request.Url,
                WorkshopCode = request.WorkshopCode,
                DeviceId = request.DeviceId,
                CompanyName = request.CompanyName,
                DeviceBarcode = request.DeviceBarcode,
                OrganizationNumber = request.OrganizationNumber,
                EmployeeNumber = request.EmployeeNumber,
                TimeoutMs = request.TimeoutMs,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = existingConfig?.CreatedAt ?? now,
                UpdatedAt = now
            };

            bool success;
            if (existingConfig == null)
            {
                success = await _postProcessingCenterConfigRepository.AddAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("创建邮政处理中心 API配置成功");
            }
            else
            {
                success = await _postProcessingCenterConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
                _logger.LogInformation("更新邮政处理中心 API配置成功");
            }
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    existingConfig == null ? "创建配置失败" : "更新配置失败",
                    existingConfig == null ? "CREATE_FAILED" : "UPDATE_FAILED"));
            }

            return Ok(ApiResponse<string>.SuccessResult(
                existingConfig == null ? "配置创建成功" : "配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新邮政处理中心配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }
}
