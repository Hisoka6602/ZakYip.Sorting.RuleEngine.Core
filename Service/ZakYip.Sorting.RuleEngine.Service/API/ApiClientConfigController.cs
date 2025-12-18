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

    public ApiClientConfigController(
        ILogger<ApiClientConfigController> logger,
        IServiceProvider serviceProvider,
        IOptions<AppSettings> appSettings,
        ISystemClock clock,
        IPostCollectionConfigRepository postCollectionConfigRepository,
        IPostProcessingCenterConfigRepository postProcessingCenterConfigRepository)
    {
        _logger = logger;
        _appSettings = appSettings.Value;
        _clock = clock;
        _postCollectionConfigRepository = postCollectionConfigRepository;
        _postProcessingCenterConfigRepository = postProcessingCenterConfigRepository;
        
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
        Description = "获取当前聚水潭ERP API客户端的配置参数",
        OperationId = "GetJushuitanErpConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<JushuitanErpConfigRequest>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<JushuitanErpConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<JushuitanErpConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<JushuitanErpConfigRequest>), 404)]
    public ActionResult<ApiResponse<JushuitanErpConfigRequest>> GetJushuitanErpConfig()
    {
        try
        {
            if (_jushuitanErpApiClient == null)
            {
                return NotFound(ApiResponse<JushuitanErpConfigRequest>.FailureResult(
                    "聚水潭ERP ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            var config = new JushuitanErpConfigRequest
            {
                Url = _jushuitanErpApiClient.Parameters.Url,
                TimeOut = _jushuitanErpApiClient.Parameters.TimeOut,
                AppKey = MaskSecret(_jushuitanErpApiClient.Parameters.AppKey),
                AppSecret = MaskSecret(_jushuitanErpApiClient.Parameters.AppSecret),
                AccessToken = MaskSecret(_jushuitanErpApiClient.Parameters.AccessToken),
                Version = _jushuitanErpApiClient.Parameters.Version,
                IsUploadWeight = _jushuitanErpApiClient.Parameters.IsUploadWeight,
                Type = _jushuitanErpApiClient.Parameters.Type,
                IsUnLid = _jushuitanErpApiClient.Parameters.IsUnLid,
                Channel = _jushuitanErpApiClient.Parameters.Channel,
                DefaultWeight = _jushuitanErpApiClient.Parameters.DefaultWeight
            };

            return Ok(ApiResponse<JushuitanErpConfigRequest>.SuccessResult(config));
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
        Description = "更新聚水潭ERP API客户端的配置参数",
        OperationId = "UpdateJushuitanErpConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public ActionResult<ApiResponse<string>> UpdateJushuitanErpConfig([FromBody] JushuitanErpConfigRequest request)
    {
        try
        {
            if (_jushuitanErpApiClient == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(
                    "聚水潭ERP ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            _jushuitanErpApiClient.Parameters.Url = request.Url;
            _jushuitanErpApiClient.Parameters.TimeOut = request.TimeOut;
            _jushuitanErpApiClient.Parameters.AppKey = request.AppKey;
            _jushuitanErpApiClient.Parameters.AppSecret = request.AppSecret;
            _jushuitanErpApiClient.Parameters.AccessToken = request.AccessToken;
            _jushuitanErpApiClient.Parameters.Version = request.Version;
            _jushuitanErpApiClient.Parameters.IsUploadWeight = request.IsUploadWeight;
            _jushuitanErpApiClient.Parameters.Type = request.Type;
            _jushuitanErpApiClient.Parameters.IsUnLid = request.IsUnLid;
            _jushuitanErpApiClient.Parameters.Channel = request.Channel;
            _jushuitanErpApiClient.Parameters.DefaultWeight = request.DefaultWeight;

            _logger.LogInformation("成功更新聚水潭ERP API配置");
            return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
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
        Description = "获取当前旺店通WMS API客户端的配置参数",
        OperationId = "GetWdtWmsConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WdtWmsConfigRequest>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<WdtWmsConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<WdtWmsConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WdtWmsConfigRequest>), 404)]
    public ActionResult<ApiResponse<WdtWmsConfigRequest>> GetWdtWmsConfig()
    {
        try
        {
            if (_wdtWmsApiClient == null)
            {
                return NotFound(ApiResponse<WdtWmsConfigRequest>.FailureResult(
                    "旺店通WMS ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            var config = new WdtWmsConfigRequest
            {
                Url = _wdtWmsApiClient.Parameters.Url,
                Sid = MaskSecret(_wdtWmsApiClient.Parameters.Sid),
                AppKey = MaskSecret(_wdtWmsApiClient.Parameters.AppKey),
                AppSecret = MaskSecret(_wdtWmsApiClient.Parameters.AppSecret),
                Method = _wdtWmsApiClient.Parameters.Method,
                TimeOut = _wdtWmsApiClient.Parameters.TimeOut,
                MustIncludeBoxBarcode = _wdtWmsApiClient.Parameters.MustIncludeBoxBarcode,
                DefaultWeight = _wdtWmsApiClient.Parameters.DefaultWeight
            };

            return Ok(ApiResponse<WdtWmsConfigRequest>.SuccessResult(config));
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
        Description = "更新旺店通WMS API客户端的配置参数",
        OperationId = "UpdateWdtWmsConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public ActionResult<ApiResponse<string>> UpdateWdtWmsConfig([FromBody] WdtWmsConfigRequest request)
    {
        try
        {
            if (_wdtWmsApiClient == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(
                    "旺店通WMS ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            _wdtWmsApiClient.Parameters.Url = request.Url;
            _wdtWmsApiClient.Parameters.Sid = request.Sid;
            _wdtWmsApiClient.Parameters.AppKey = request.AppKey;
            _wdtWmsApiClient.Parameters.AppSecret = request.AppSecret;
            _wdtWmsApiClient.Parameters.Method = request.Method;
            _wdtWmsApiClient.Parameters.TimeOut = request.TimeOut;
            _wdtWmsApiClient.Parameters.MustIncludeBoxBarcode = request.MustIncludeBoxBarcode;
            _wdtWmsApiClient.Parameters.DefaultWeight = request.DefaultWeight;

            _logger.LogInformation("成功更新旺店通WMS API配置");
            return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
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
        Description = "获取当前旺店通ERP旗舰版 API客户端的配置参数",
        OperationId = "GetWdtErpFlagshipConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "成功返回配置", typeof(ApiResponse<WdtErpFlagshipConfigRequest>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<WdtErpFlagshipConfigRequest>))]
    [ProducesResponseType(typeof(ApiResponse<WdtErpFlagshipConfigRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<WdtErpFlagshipConfigRequest>), 404)]
    public ActionResult<ApiResponse<WdtErpFlagshipConfigRequest>> GetWdtErpFlagshipConfig()
    {
        try
        {
            if (_wdtErpFlagshipApiClient == null)
            {
                return NotFound(ApiResponse<WdtErpFlagshipConfigRequest>.FailureResult(
                    "旺店通ERP旗舰版 ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            var config = new WdtErpFlagshipConfigRequest
            {
                Url = _wdtErpFlagshipApiClient.Parameters.Url,
                Key = MaskSecret(_wdtErpFlagshipApiClient.Parameters.Key),
                Appsecret = MaskSecret(_wdtErpFlagshipApiClient.Parameters.Appsecret),
                Sid = MaskSecret(_wdtErpFlagshipApiClient.Parameters.Sid),
                Method = _wdtErpFlagshipApiClient.Parameters.Method,
                V = _wdtErpFlagshipApiClient.Parameters.V,
                Salt = MaskSecret(_wdtErpFlagshipApiClient.Parameters.Salt),
                PackagerId = _wdtErpFlagshipApiClient.Parameters.PackagerId,
                PackagerNo = _wdtErpFlagshipApiClient.Parameters.PackagerNo,
                OperateTableName = _wdtErpFlagshipApiClient.Parameters.OperateTableName,
                Force = _wdtErpFlagshipApiClient.Parameters.Force,
                TimeOut = _wdtErpFlagshipApiClient.Parameters.TimeOut
            };

            return Ok(ApiResponse<WdtErpFlagshipConfigRequest>.SuccessResult(config));
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
        Description = "更新旺店通ERP旗舰版 API客户端的配置参数",
        OperationId = "UpdateWdtErpFlagshipConfig",
        Tags = new[] { "ApiClientConfig" }
    )]
    [SwaggerResponse(200, "配置更新成功", typeof(ApiResponse<string>))]
    [SwaggerResponse(404, "ApiClient未配置", typeof(ApiResponse<string>))]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public ActionResult<ApiResponse<string>> UpdateWdtErpFlagshipConfig([FromBody] WdtErpFlagshipConfigRequest request)
    {
        try
        {
            if (_wdtErpFlagshipApiClient == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(
                    "旺店通ERP旗舰版 ApiClient未配置", "CLIENT_NOT_CONFIGURED"));
            }

            _wdtErpFlagshipApiClient.Parameters.Url = request.Url;
            _wdtErpFlagshipApiClient.Parameters.Key = request.Key;
            _wdtErpFlagshipApiClient.Parameters.Appsecret = request.Appsecret;
            _wdtErpFlagshipApiClient.Parameters.Sid = request.Sid;
            _wdtErpFlagshipApiClient.Parameters.Method = request.Method;
            _wdtErpFlagshipApiClient.Parameters.V = request.V;
            _wdtErpFlagshipApiClient.Parameters.Salt = request.Salt;
            _wdtErpFlagshipApiClient.Parameters.PackagerId = request.PackagerId;
            _wdtErpFlagshipApiClient.Parameters.PackagerNo = request.PackagerNo;
            _wdtErpFlagshipApiClient.Parameters.OperateTableName = request.OperateTableName;
            _wdtErpFlagshipApiClient.Parameters.Force = request.Force;
            _wdtErpFlagshipApiClient.Parameters.TimeOut = request.TimeOut;

            _logger.LogInformation("成功更新旺店通ERP旗舰版 API配置");
            return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
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
            
            if (existingConfig == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(
                    "邮政分揽投机构配置不存在", "CONFIG_NOT_FOUND"));
            }

            var updatedConfig = existingConfig with
            {
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
                UpdatedAt = _clock.LocalNow
            };

            var success = await _postCollectionConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    "更新配置失败", "UPDATE_FAILED"));
            }

            _logger.LogInformation("成功更新邮政分揽投机构 API配置");
            return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
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
            
            if (existingConfig == null)
            {
                return NotFound(ApiResponse<string>.FailureResult(
                    "邮政处理中心配置不存在", "CONFIG_NOT_FOUND"));
            }

            var updatedConfig = existingConfig with
            {
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
                UpdatedAt = _clock.LocalNow
            };

            var success = await _postProcessingCenterConfigRepository.UpdateAsync(updatedConfig).ConfigureAwait(false);
            
            if (!success)
            {
                return StatusCode(500, ApiResponse<string>.FailureResult(
                    "更新配置失败", "UPDATE_FAILED"));
            }

            _logger.LogInformation("成功更新邮政处理中心 API配置");
            return Ok(ApiResponse<string>.SuccessResult("配置更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新邮政处理中心配置时发生错误");
            return StatusCode(500, ApiResponse<string>.FailureResult(
                "更新配置失败", "UPDATE_CONFIG_FAILED"));
        }
    }
}
