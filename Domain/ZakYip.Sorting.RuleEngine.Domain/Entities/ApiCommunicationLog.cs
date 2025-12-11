using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// API通信日志实体
/// API communication log entity
/// </summary>
public class ApiCommunicationLog : BaseApiCommunication
{
    /// <summary>
    /// 日志ID（自增主键）
    /// Log ID (auto-increment primary key)
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 通信类型（HTTP为主，记录API调用方式）
    /// Communication type (mainly HTTP, records API call method)
    /// </summary>
    public CommunicationType CommunicationType { get; set; } = CommunicationType.Http;

    /// <summary>
    /// 是否成功
    /// Is successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
