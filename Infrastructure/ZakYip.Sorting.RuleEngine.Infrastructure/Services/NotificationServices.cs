using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 告警通知服务实现
/// Alert notification service implementation
/// </summary>
public class AlertNotificationService : IAlertNotificationService
{
    private readonly IEmailNotificationService? _emailService;
    private readonly ISmsNotificationService? _smsService;
    private readonly IWeChatWorkNotificationService? _weChatService;
    private readonly ILogger<AlertNotificationService> _logger;

    // 通知配置
    private readonly List<string> _emailRecipients = new();
    private readonly List<string> _smsRecipients = new();
    private readonly bool _enableEmailNotification = false;
    private readonly bool _enableSmsNotification = false;
    private readonly bool _enableWeChatNotification = false;

    public AlertNotificationService(
        IEmailNotificationService? emailService,
        ISmsNotificationService? smsService,
        IWeChatWorkNotificationService? weChatService,
        ILogger<AlertNotificationService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _weChatService = weChatService;
        _logger = logger;

        // TODO: 从配置读取收件人列表和开关
        // 这里使用默认值，实际应该从配置文件或数据库读取
    }

    /// <inheritdoc />
    public async Task<bool> SendAlertAsync(MonitoringAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始发送告警通知，类型: {Type}, 严重级别: {Severity}", 
                alert.Type, alert.Severity);

            var tasks = new List<Task<bool>>();

            // 根据告警严重级别决定通知渠道
            // Critical: 邮件 + 短信 + 企业微信
            // Warning: 邮件 + 企业微信
            // Info: 仅企业微信

            if (_enableEmailNotification && _emailService != null && 
                (alert.Severity == AlertSeverity.Critical || alert.Severity == AlertSeverity.Warning))
            {
                tasks.Add(_emailService.SendAlertEmailAsync(alert, _emailRecipients, cancellationToken));
            }

            if (_enableSmsNotification && _smsService != null && alert.Severity == AlertSeverity.Critical)
            {
                tasks.Add(_smsService.SendAlertSmsAsync(alert, _smsRecipients, cancellationToken));
            }

            if (_enableWeChatNotification && _weChatService != null)
            {
                tasks.Add(_weChatService.SendAlertMessageAsync(alert, cancellationToken));
            }

            if (tasks.Count == 0)
            {
                _logger.LogWarning("没有启用任何通知渠道或没有可用的通知服务");
                return false;
            }

            var results = await Task.WhenAll(tasks);
            var allSuccess = results.All(r => r);

            if (allSuccess)
            {
                _logger.LogInformation("告警通知发送成功，告警ID: {AlertId}", alert.AlertId);
            }
            else
            {
                _logger.LogWarning("部分告警通知发送失败，告警ID: {AlertId}", alert.AlertId);
            }

            return allSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送告警通知异常，告警ID: {AlertId}", alert.AlertId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(int SuccessCount, int FailedCount)> SendBatchAlertsAsync(
        List<MonitoringAlert> alerts, 
        CancellationToken cancellationToken = default)
    {
        int successCount = 0;
        int failedCount = 0;

        try
        {
            _logger.LogInformation("开始批量发送告警通知，总数: {Count}", alerts.Count);

            foreach (var alert in alerts)
            {
                var success = await SendAlertAsync(alert, cancellationToken);
                if (success)
                    successCount++;
                else
                    failedCount++;
            }

            _logger.LogInformation(
                "批量告警通知发送完成，成功: {Success}, 失败: {Failed}",
                successCount, failedCount);

            return (successCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量发送告警通知异常");
            return (successCount, failedCount);
        }
    }
}

/// <summary>
/// 邮件通知服务实现（基础实现）
/// Email notification service implementation
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("发送邮件: 收件人={To}, 主题={Subject}", to, subject);

            // TODO: 实际的邮件发送逻辑
            // 可以使用 MailKit 或 System.Net.Mail
            // 这里仅做日志记录

            await Task.Delay(100, cancellationToken); // 模拟发送延迟

            _logger.LogInformation("邮件发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件发送失败");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendAlertEmailAsync(
        MonitoringAlert alert, 
        List<string> recipients,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"[{alert.Severity}] {alert.Title}";
            var body = FormatAlertEmailBody(alert);

            var tasks = recipients.Select(recipient => 
                SendEmailAsync(recipient, subject, body, true, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送告警邮件失败");
            return false;
        }
    }

    private static string FormatAlertEmailBody(MonitoringAlert alert)
    {
        return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .alert-box {{ border: 2px solid #dc3545; padding: 20px; border-radius: 5px; }}
        .alert-critical {{ background-color: #f8d7da; }}
        .alert-warning {{ background-color: #fff3cd; }}
        .alert-info {{ background-color: #d1ecf1; }}
        h2 {{ color: #dc3545; }}
        .details {{ margin-top: 15px; }}
        .detail-row {{ margin: 5px 0; }}
        .label {{ font-weight: bold; }}
    </style>
</head>
<body>
    <div class='alert-box alert-{alert.Severity.ToString().ToLower()}'>
        <h2>{alert.Title}</h2>
        <div class='details'>
            <div class='detail-row'><span class='label'>告警类型:</span> {alert.Type}</div>
            <div class='detail-row'><span class='label'>严重级别:</span> {alert.Severity}</div>
            <div class='detail-row'><span class='label'>告警时间:</span> {alert.AlertTime:yyyy-MM-dd HH:mm:ss}</div>
            <div class='detail-row'><span class='label'>告警消息:</span> {alert.Message}</div>
            {(alert.CurrentValue.HasValue ? $"<div class='detail-row'><span class='label'>当前值:</span> {alert.CurrentValue:F2}</div>" : "")}
            {(alert.ThresholdValue.HasValue ? $"<div class='detail-row'><span class='label'>阈值:</span> {alert.ThresholdValue:F2}</div>" : "")}
            {(!string.IsNullOrEmpty(alert.ResourceId) ? $"<div class='detail-row'><span class='label'>资源ID:</span> {alert.ResourceId}</div>" : "")}
        </div>
    </div>
</body>
</html>";
    }
}

/// <summary>
/// 短信通知服务实现（基础实现）
/// SMS notification service implementation
/// </summary>
public class SmsNotificationService : ISmsNotificationService
{
    private readonly ILogger<SmsNotificationService> _logger;

    public SmsNotificationService(ILogger<SmsNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendSmsAsync(
        string phoneNumber, 
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("发送短信: 手机号={Phone}, 内容={Message}", phoneNumber, message);

            // TODO: 实际的短信发送逻辑
            // 可以对接阿里云短信、腾讯云短信等服务

            await Task.Delay(100, cancellationToken); // 模拟发送延迟

            _logger.LogInformation("短信发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "短信发送失败");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendAlertSmsAsync(
        MonitoringAlert alert, 
        List<string> phoneNumbers,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = FormatAlertSmsMessage(alert);

            var tasks = phoneNumbers.Select(phone => 
                SendSmsAsync(phone, message, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送告警短信失败");
            return false;
        }
    }

    private static string FormatAlertSmsMessage(MonitoringAlert alert)
    {
        return $"【告警通知】{alert.Title}\n" +
               $"级别: {alert.Severity}\n" +
               $"时间: {alert.AlertTime:MM-dd HH:mm}\n" +
               $"消息: {alert.Message}";
    }
}

/// <summary>
/// 企业微信通知服务实现（基础实现）
/// WeChat Work notification service implementation
/// </summary>
public class WeChatWorkNotificationService : IWeChatWorkNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeChatWorkNotificationService> _logger;
    private readonly string _webhookUrl = string.Empty; // 从配置读取

    public WeChatWorkNotificationService(
        HttpClient httpClient,
        ILogger<WeChatWorkNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendTextMessageAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                _logger.LogWarning("企业微信Webhook地址未配置");
                return false;
            }

            _logger.LogInformation("发送企业微信消息: {Content}", content);

            // TODO: 实际的企业微信消息发送逻辑
            // 使用企业微信机器人Webhook API

            await Task.Delay(100, cancellationToken); // 模拟发送延迟

            _logger.LogInformation("企业微信消息发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "企业微信消息发送失败");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendMarkdownMessageAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                _logger.LogWarning("企业微信Webhook地址未配置");
                return false;
            }

            _logger.LogInformation("发送企业微信Markdown消息");

            // TODO: 实际的企业微信Markdown消息发送逻辑

            await Task.Delay(100, cancellationToken); // 模拟发送延迟

            _logger.LogInformation("企业微信Markdown消息发送成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "企业微信Markdown消息发送失败");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendAlertMessageAsync(
        MonitoringAlert alert,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var markdown = FormatAlertMarkdown(alert);
            return await SendMarkdownMessageAsync(markdown, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送告警企业微信消息失败");
            return false;
        }
    }

    private static string FormatAlertMarkdown(MonitoringAlert alert)
    {
        var severityColor = alert.Severity switch
        {
            AlertSeverity.Critical => "warning",
            AlertSeverity.Warning => "warning",
            _ => "info"
        };

        return $@"## {alert.Title}

> **告警级别**: <font color=""{severityColor}"">{alert.Severity}</font>
> **告警类型**: {alert.Type}
> **告警时间**: {alert.AlertTime:yyyy-MM-dd HH:mm:ss}

**告警消息**: {alert.Message}

{(alert.CurrentValue.HasValue ? $"**当前值**: {alert.CurrentValue:F2}\n" : "")}
{(alert.ThresholdValue.HasValue ? $"**阈值**: {alert.ThresholdValue:F2}\n" : "")}
{(!string.IsNullOrEmpty(alert.ResourceId) ? $"**资源ID**: {alert.ResourceId}\n" : "")}

请及时处理！";
    }
}
