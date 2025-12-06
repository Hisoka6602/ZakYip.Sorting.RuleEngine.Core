using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.PostCollection;

/// <summary>
/// 邮政分揽投机构API客户端实现
/// Postal Collection/Delivery Institution API client implementation
/// 参考: https://gist.github.com/Hisoka6602/dc321e39f3dbece14129d28e65480a8e (PostInApi)
/// 使用SOAP协议进行通信
/// </summary>
/// <remarks>
/// 此类继承自 BasePostalApiClient，提供邮政分揽投机构特定的功能。
/// 共享的 SOAP 通信逻辑已移至基类以消除代码重复。
/// This class inherits from BasePostalApiClient and provides postal collection institution specific functionality.
/// Shared SOAP communication logic has been moved to the base class to eliminate code duplication.
/// </remarks>
public class PostCollectionApiClient : BasePostalApiClient
{
    /// <summary>
    /// 获取客户端类型名称
    /// Get client type name
    /// </summary>
    protected override string ClientTypeName => "邮政分揽投机构";

    public PostCollectionApiClient(
        HttpClient httpClient,
        ILogger<PostCollectionApiClient> logger)
        : base(httpClient, logger)
    {
    }
}
