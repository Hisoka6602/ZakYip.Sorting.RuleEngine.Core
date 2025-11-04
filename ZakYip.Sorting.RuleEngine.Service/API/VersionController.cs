using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 版本信息控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("系统版本信息接口")]
public class VersionController : ControllerBase
{
    /// <summary>
    /// 获取系统版本信息
    /// </summary>
    /// <returns>版本信息</returns>
    /// <response code="200">成功返回版本信息</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "获取系统版本信息",
        Description = "获取系统的版本号、构建日期等详细版本信息",
        OperationId = "GetVersion",
        Tags = new[] { "Version" }
    )]
    [SwaggerResponse(200, "成功返回版本信息", typeof(ApiResponse<VersionResponseDto>))]
    [ProducesResponseType(typeof(ApiResponse<VersionResponseDto>), 200)]
    public ActionResult<ApiResponse<VersionResponseDto>> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

        var versionData = new VersionResponseDto
        {
            Version = version?.ToString() ?? "1.13.0",
            ProductVersion = fileVersionInfo.ProductVersion ?? "1.13.0",
            FileVersion = fileVersionInfo.FileVersion ?? "1.13.0.0",
            ProductName = fileVersionInfo.ProductName ?? "ZakYip 分拣规则引擎",
            CompanyName = fileVersionInfo.CompanyName ?? "ZakYip",
            Description = "ZakYip分拣规则引擎系统 - 高性能包裹分拣规则引擎",
            BuildDate = GetBuildDate(assembly).ToString("yyyy-MM-dd HH:mm:ss"),
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        };

        return Ok(ApiResponse<VersionResponseDto>.SuccessResult(versionData));
    }

    /// <summary>
    /// 获取构建日期
    /// </summary>
    private static DateTime GetBuildDate(Assembly assembly)
    {
        const string BuildVersionMetadataPrefix = "+build";
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }
        }

        // 如果无法解析，返回程序集文件的最后修改时间
        return System.IO.File.GetLastWriteTime(assembly.Location);
    }
}
