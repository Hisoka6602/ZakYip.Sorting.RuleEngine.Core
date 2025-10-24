using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ZakYip.Sorting.RuleEngine.Service.API;

/// <summary>
/// 版本信息控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    /// <summary>
    /// 获取系统版本信息
    /// </summary>
    /// <returns>版本信息</returns>
    [HttpGet]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

        return Ok(new
        {
            version = version?.ToString() ?? "1.7.0",
            productVersion = fileVersionInfo.ProductVersion ?? "1.7.0",
            fileVersion = fileVersionInfo.FileVersion ?? "1.7.0.0",
            productName = fileVersionInfo.ProductName ?? "ZakYip 分拣规则引擎",
            companyName = fileVersionInfo.CompanyName ?? "ZakYip",
            description = "ZakYip分拣规则引擎系统 - 高性能包裹分拣规则引擎",
            buildDate = GetBuildDate(assembly).ToString("yyyy-MM-dd HH:mm:ss"),
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        });
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
