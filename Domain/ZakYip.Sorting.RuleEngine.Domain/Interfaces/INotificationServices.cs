using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 告警通知服务接口
/// Alert notification service interface
/// </summary>
public interface IAlertNotificationService
{
    /// <summary>
    /// 发送告警通知
    /// Send alert notification
    /// </summary>
    /// <param name="alert">告警信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发送告警通知
    /// Send batch alert notifications
    /// </summary>
    /// <param name="alerts">告警信息列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果统计</returns>
    Task<(int SuccessCount, int FailedCount)> SendBatchAlertsAsync(
        List<MonitoringAlert> alerts, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 邮件通知服务接口
/// Email notification service interface
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// 发送邮件
    /// Send email
    /// </summary>
    /// <param name="to">收件人邮箱地址</param>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文（支持HTML）</param>
    /// <param name="isHtml">是否为HTML格式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送告警邮件
    /// Send alert email
    /// </summary>
    /// <param name="alert">告警信息</param>
    /// <param name="recipients">收件人列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAlertEmailAsync(
        MonitoringAlert alert, 
        List<string> recipients,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 短信通知服务接口
/// SMS notification service interface
/// </summary>
public interface ISmsNotificationService
{
    /// <summary>
    /// 发送短信
    /// Send SMS
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="message">短信内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendSmsAsync(
        string phoneNumber, 
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送告警短信
    /// Send alert SMS
    /// </summary>
    /// <param name="alert">告警信息</param>
    /// <param name="phoneNumbers">手机号码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAlertSmsAsync(
        MonitoringAlert alert, 
        List<string> phoneNumbers,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 企业微信通知服务接口
/// WeChat Work notification service interface
/// </summary>
public interface IWeChatWorkNotificationService
{
    /// <summary>
    /// 发送文本消息
    /// Send text message
    /// </summary>
    /// <param name="content">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendTextMessageAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送Markdown消息
    /// Send markdown message
    /// </summary>
    /// <param name="content">Markdown内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendMarkdownMessageAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送告警消息
    /// Send alert message
    /// </summary>
    /// <param name="alert">告警信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAlertMessageAsync(
        MonitoringAlert alert,
        CancellationToken cancellationToken = default);
}
